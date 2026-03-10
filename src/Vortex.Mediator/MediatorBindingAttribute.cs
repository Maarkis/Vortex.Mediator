namespace Vortex.Mediator;

/// <summary>
/// Marks an assembly as containing a generated mediator binding implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MediatorBindingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorBindingAttribute"/> class.
    /// </summary>
    /// <param name="bindingType">The generated binding type exposed by the assembly.</param>
    public MediatorBindingAttribute(Type bindingType)
    {
        BindingType = bindingType;
    }

    /// <summary>
    /// Gets the generated binding type exposed by the assembly.
    /// </summary>
    public Type BindingType { get; }
}
