using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aether.Scripting
{
    /// <summary>
    /// Inserts <c>__line(n);</c> before each statement so Roslyn scripts can
    /// pause on a breakpoint before the statement runs. <c>n</c> is the
    /// original 1-based source line.</summary>
    internal static class CSharpLineHooks
    {
        public static string Inject(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source ?? string.Empty);
            SyntaxNode root = new Rewriter().Visit(tree.GetRoot());
            return root.ToFullString();
        }

        private sealed class Rewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return Wrap(node);
            }

            public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                return Wrap(node);
            }

            private static SyntaxNode Wrap(StatementSyntax node)
            {
                int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                StatementSyntax hook = SyntaxFactory.ParseStatement("__line(" + line + ");");
                return SyntaxFactory.Block(hook, node);
            }
        }
    }
}
