using System;
using System.Collections.Generic;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    public partial class Criteria
    {
        public static bool TryParse(string filter, ResolveField resolveField, ResolveConstant resolveConstant, out ICriteria criteria)
        {
            try
            {
                criteria = Parse(filter, resolveField, resolveConstant);
                return true;
            }
            catch (CriteriaParseException)
            {
                criteria = null;
                return false;
            }
        }

        public static ICriteria Parse(string filter, ResolveField resolveField, ResolveConstant resolveConstant)
        {
            ICriteria criteria = new Criteria(CriteriaType.Empty);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (!(filter.StartsWith("(") && filter.EndsWith(")")))
                {
                    filter = $"({filter})";
                }

                var tokenizer = new StringTokenizer(filter)
                {
                    IgnoreWhiteSpace = true,
                    SymbolChars = new[] { '(', ')', ',' },
                    TreatNumberAsLetterChars = true,
                    TreatUnknownAsLetterChars = true
                };

                var tokens = tokenizer.ReadAll();

                var counter = new Counter();

                criteria = BuildCriteria(counter, resolveField, resolveConstant, tokens);
            }

            return criteria;
        }

        private static ICriteria BuildCriteria(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, Token[] tokens)
        {
            var rootNode = new Node(NodeType.Unknown, null);
            var currentNode = rootNode.AddChild(NodeType.Unknown);

            for (var index = 0; index < tokens.Length; index++)
            {
                var previousToken = tokens.ElementAtOrDefault(index - 1);
                var token = tokens[index];

                switch (token.Value)
                {
                    case "(":
                        if (IsAFunction(previousToken))
                        {
                            // Remove last token as it was the function name
                            currentNode.Tokens.RemoveAt(currentNode.Tokens.Count - 1);

                            // Don't capture the parenthesis

                            // Start new function
                            var functionNode = new Node(NodeType.Function, currentNode, previousToken);
                            currentNode.Children.Add(functionNode);

                            currentNode = functionNode;
                        }
                        else
                        {
                            // Start new group
                            var nodeType = GetGroupNodeType(tokens, index);

                            var groupNode = currentNode.AddChild(nodeType);

                            currentNode = groupNode.AddChild(NodeType.Unknown);
                        }

                        break;

                    case ")":
                        if (currentNode.NodeType == NodeType.Function)
                        {
                            // Don't capture the parenthesis
                            // function closed, start a new node
                            currentNode = currentNode.Parent.AddChild(NodeType.Unknown);
                        }
                        else
                        {
                            var groupNode = currentNode.Parent.Parent;
                            var groupNodeParent = groupNode.Parent;

                            currentNode = groupNodeParent.AddChild(NodeType.Unknown);
                        }

                        break;

                    case "and":
                    case "or":
                        {
                            var parent = currentNode.Parent;
                            parent.AddChild(NodeType.Binary, token);

                            currentNode = parent.AddChild(NodeType.Unknown);
                        }
                        break;

                    default:
                        currentNode.Tokens.Add(token);

                        break;
                }
            }

            rootNode = GetRootNode(rootNode);
            
            var criteria = BuildCriteria(counter, resolveField, resolveConstant, rootNode);
            return criteria;
        }

        private static ICriteria BuildCriteria(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, Node node)
        {
            if (node.Tokens.Count == 0 && node.NodeType == NodeType.Unknown && node.Children.Count == 1)
            {
                return BuildCriteria(counter, resolveField, resolveConstant, node.Children[0]);
            }

            if (node.NodeType == NodeType.BinaryGroup)
            {
                var groupNode = GroupNode(node);
                var binaryNode = groupNode.Children.FirstOrDefault(c => c.NodeType == NodeType.Binary);

                if (binaryNode != null)
                {
                    var binaryCriteria = binaryNode.Tokens[0].Value == "and"
                        ? And(new ICriteria[0])
                        : Or(new ICriteria[0]);

                    var nodes = new List<Node>();

                    foreach (var child in groupNode.Children)
                    {
                        if (child.NodeType == NodeType.Binary)
                        {
                            CreateCriteriaFromNodes(counter, resolveField, resolveConstant, nodes, binaryCriteria);

                            nodes = new List<Node>();
                        }
                        else
                        {
                            nodes.Add(child);
                        }
                    }

                    CreateCriteriaFromNodes(counter, resolveField, resolveConstant, nodes, binaryCriteria);

                    return binaryCriteria;
                }

                System.Diagnostics.Debug.WriteLine(node);
            }

            var expression = ToExpression(counter, resolveField, resolveConstant, node);
            return FromExpression(expression);
        }

        private static NodeType GetGroupNodeType(Token[] tokens, int index)
        {
            return IsBinaryGroup(tokens.Skip(index + 1).ToArray())
                ? NodeType.BinaryGroup
                : NodeType.Group;
        }

        private static bool IsBinaryGroup(Token[] tokens)
        {
            int nesting = 0;

            foreach (var token in tokens)
            {
                if (token.Value == "(")
                {
                    nesting++;
                }
                else if (token.Value == ")")
                {
                    nesting--;
                }
                else if (nesting == 0 && token.Value == "or" || token.Value == "and")
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateCriteriaFromNodes(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, List<Node> nodes, ICriteria binaryCriteria)
        {
            if (nodes.Count == 1)
            {
                binaryCriteria.Add(BuildCriteria(counter, resolveField, resolveConstant, nodes[0]));
            }
            else if (nodes.Count > 0)
            {
                binaryCriteria.Add(ToExpression(counter, resolveField, resolveConstant, nodes.ToArray()).ToCriteria());
            }
        }

        private static Node GroupNode(Node node)
        {
            if (node.Tokens.Count > 0)
            {
                return node;
            }

            if (node.Children.Any(c => c.NodeType == NodeType.Binary))
            {
                return node;
            }

            if (node.Children.Count == 1)
            {
                return GroupNode(node.Children[0]);
            }

            return node;
        }

        private static IExpression ToExpression(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, params Node[] nodes)
        {
            // Flatten
            var sqlFragments = nodes.SelectMany(node => Flatten(counter, resolveField, resolveConstant, node)).ToArray();

            if (nodes.Length == 1 && sqlFragments.Length == 1 && sqlFragments[0] is GroupExpression)
            {
                return (GroupExpression)sqlFragments[0];
            }

            var builder = new OperatorBuilder(counter, resolveField);

            foreach (var sqlFragment in sqlFragments)
            {
                builder.Add(sqlFragment);
            }

            return builder.ToExpression();
        }

        private static ISqlFragment TokenToSqlFragment(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, Token token)
        {
            var value = token.Value;

            var op = Operator.Parse(value);

            if (op != null)
            {
                return op;
            }

            if (value == ",")
            {
                return new Comma();
            }

            if (token.Kind == TokenKind.QuotedString)
            {
                var parameterValue = value.Substring(1).Substring(0, value.Length - 2);
                return new Parameter("Filter" + counter.Next(), parameterValue);
            }

            var valueAsNumber = GetValueAsNumber(value);
            if (valueAsNumber != null)
            {
                return new Parameter("Filter" + counter.Next(), valueAsNumber);
            }

            var filterField = resolveField(value);
            if (filterField != null)
            {
                return filterField.Identifier;
            }

            var constant = resolveConstant(value);

            if (constant != null)
            {
                return new Parameter("Filter" + counter.Next(), constant);
            }

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return new Parameter("Filter" + counter.Next(), true);
            }

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return new Parameter("Filter" + counter.Next(), false);
            }

            if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return new Parameter("Filter" + counter.Next(), DBNull.Value);
            }

            return new UnparsedTokenFragment(token);
        }

        private static object GetValueAsNumber(string value)
        {
            object convertedValue = value.ConvertSafe<int?>();
            if (convertedValue != null)
            {
                return convertedValue;
            }

            convertedValue = value.ConvertSafe<long?>();
            if (convertedValue != null)
            {
                return convertedValue;
            }

            convertedValue = value.ConvertSafe<decimal?>();
            if (convertedValue != null)
            {
                return convertedValue;
            }

            convertedValue = value.ConvertSafe<float?>();

            return convertedValue;
        }

        private static IEnumerable<ISqlFragment> Flatten(Counter counter, ResolveField resolveField, ResolveConstant resolveConstant, Node node)
        {
            if (node.NodeType == NodeType.Group)
            {
                var groupExpression = new GroupExpression(ToExpression(counter, resolveField, resolveConstant, node.Children.ToArray()));
                yield return groupExpression;
                yield break;
            }

            foreach (var token in node.Tokens)
            {
                yield return TokenToSqlFragment(counter, resolveField, resolveConstant, token);
            }

            foreach (var child in node.Children)
            {
                if (child.NodeType == NodeType.Group)
                {
                    yield return ToExpression(counter, resolveField, resolveConstant, child);
                }
                else
                {
                    foreach (var sqlFragment in Flatten(counter, resolveField, resolveConstant, child))
                    {
                        yield return sqlFragment;
                    }
                }
            }
        }

        private static Node GetRootNode(Node node)
        {
            var actions = new List<Action>();

            CleanupNodes(actions, node);

            foreach (var action in actions)
            {
                action();
            }

            return node;
        }

        private static void CleanupNodes(List<Action> actions, Node node)
        {
            if (node.Children.Count == 0 && node.Tokens.Count == 0 && node.Parent != null)
            {
                actions.Add(() => node.Parent.Children.Remove(node));
            }
            else
            {
                foreach (var child in node.Children)
                {
                    CleanupNodes(actions, child);
                }
            }
        }

        private static bool IsAFunction(Token token)
        {
            if (token == null || token.Kind != TokenKind.Word)
            {
                return false;
            }

            var functionName = token.Value;

            return Operator.FunctionNames.Contains(functionName);
        }

        private class Node
        {
            public Node(NodeType nodeType, Node parent, params Token[] tokens)
            {
                Children = new List<Node>();
                Tokens = new List<Token>(tokens);
                NodeType = nodeType;
                Parent = parent;
            }

            public Node Parent { get; private set; }

            public List<Node> Children { get; private set; }

            public List<Token> Tokens { get; private set; }

            public NodeType NodeType { get; private set; }

            public override string ToString()
            {
                return /*"[" + NodeType.ToString().Substring(0, 1) + "," + Children.Count + "]" +*/ string.Join(" ", Tokens.Select(t => t.Value));
            }

            public static string DumpXml(Node node)
            {
                if (node.Tokens.Count == 0 && node.NodeType == NodeType.Unknown && node.Children.Count == 1)
                {
                    return DumpXml(node.Children[0]);
                }

                var dump = node.ToString();

                if (node.Children.Count > 0)
                {
                    dump += string.Join(" ", node.Children.Select(DumpXml));
                }

                var nodeType = node.NodeType.ToString().ToLowerInvariant();

                return "<" + nodeType + ">" + dump + "</" + nodeType + ">";
            }

            public Node AddChild(NodeType nodeType, params Token[] tokens)
            {
                var child = new Node(nodeType, this, tokens);
                Children.Add(child);
                return child;
            }
        }

        private enum NodeType
        {
            Unknown,
            Binary,
            BinaryGroup,
            Group,
            Function
        }
    }
}
