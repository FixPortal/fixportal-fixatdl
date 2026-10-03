using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Tests.Model;

public class ModelUtilsTests
{
    [Fact]
    public void VisitHelper_keeps_separate_cache_entries_for_visitor_implementations()
    {
        var target = new Target();
        var first = new FirstVisitor();
        var second = new SecondVisitor();

        ModelUtils.VisitHelper(typeof(IVisitor), first, target).Should().BeTrue();
        ModelUtils.VisitHelper(typeof(IVisitor), second, target).Should().BeTrue();

        first.Visited.Should().BeTrue();
        second.Visited.Should().BeTrue();
    }

    public interface IVisitor;

    public sealed class Target;

    public sealed class FirstVisitor : IVisitor
    {
        public bool Visited { get; private set; }

        public void Visit(Target target)
        {
            ArgumentNullException.ThrowIfNull(target);
            Visited = true;
        }
    }

    public sealed class SecondVisitor : IVisitor
    {
        public bool Visited { get; private set; }

        public void Visit(Target target)
        {
            ArgumentNullException.ThrowIfNull(target);
            Visited = true;
        }
    }
}
