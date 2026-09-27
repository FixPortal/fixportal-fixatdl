using System.Reflection;
using System.Reflection.Emit;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Tests.Utility;

/// <summary>
/// Tests that <see cref="ModelUtils.VisitHelper"/> keys its reflection cache by <see cref="Type"/>
/// identity, not <see cref="Type.FullName"/>: two visitor types with identical full names from
/// different assemblies would collide on a name-based key, and the second call would invoke the
/// first type's cached <see cref="MethodInfo"/>, throwing a <see cref="TargetException"/>.
/// </summary>
public class ModelUtilsTests
{
    [Fact]
    public void VisitHelper_invokes_own_visit_for_same_full_name_visitors_from_different_assemblies()
    {
        object firstVisitor = CreateDynamicVisitor(nameof(DynamicVisitProbe.RecordFirst));
        object secondVisitor = CreateDynamicVisitor(nameof(DynamicVisitProbe.RecordSecond));
        var target = new VisitTarget();
        DynamicVisitProbe.Reset();

        // The premise of the repro: identical FullName, distinct Type identity.
        firstVisitor.GetType().FullName.Should().Be(secondVisitor.GetType().FullName);
        firstVisitor.GetType().Should().NotBe(secondVisitor.GetType());

        // Warm the cache with the first visitor, then invoke the second: a FullName-keyed cache
        // collides on all three names and the second call would reuse the first type's MethodInfo.
        ModelUtils.VisitHelper(typeof(IVisitor), firstVisitor, target).Should().BeTrue();
        ModelUtils.VisitHelper(typeof(IVisitor), secondVisitor, target).Should().BeTrue();

        DynamicVisitProbe.FirstVisitCount.Should().Be(1);
        DynamicVisitProbe.SecondVisitCount.Should().Be(1);
    }

    private static object CreateDynamicVisitor(string recordMethodName)
    {
        // Each visitor lives in its own dynamic assembly (same assembly name, in fact) and defines
        // a type with the SAME FullName; only Type identity distinguishes the two.
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("ModelUtilsTests.DynamicVisitorAssembly"),
            AssemblyBuilderAccess.Run
        );
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicVisitorModule");
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            "DynamicVisitors.SameFullNameVisitor",
            TypeAttributes.Public | TypeAttributes.Class
        );

        MethodBuilder visitBuilder = typeBuilder.DefineMethod(
            "Visit",
            MethodAttributes.Public,
            returnType: typeof(void),
            parameterTypes: [typeof(VisitTarget)]
        );

        MethodInfo recordMethod = typeof(DynamicVisitProbe).GetMethod(recordMethodName)!;
        ILGenerator il = visitBuilder.GetILGenerator();
        il.Emit(OpCodes.Call, recordMethod);
        il.Emit(OpCodes.Ret);

        Type dynamicVisitorType = typeBuilder.CreateType();
        return Activator.CreateInstance(dynamicVisitorType)!;
    }

    public interface IVisitor;

    /// <summary>Marker target type the dynamic <c>Visit</c> overloads accept.</summary>
    public sealed class VisitTarget
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>
    /// Call target for the dynamically emitted <c>Visit</c> methods, recording which visitor's
    /// method actually ran.
    /// </summary>
    public static class DynamicVisitProbe
    {
        public static int FirstVisitCount { get; private set; }

        public static int SecondVisitCount { get; private set; }

        public static void Reset()
        {
            FirstVisitCount = 0;
            SecondVisitCount = 0;
        }

        public static void RecordFirst() => FirstVisitCount++;

        public static void RecordSecond() => SecondVisitCount++;
    }
}
