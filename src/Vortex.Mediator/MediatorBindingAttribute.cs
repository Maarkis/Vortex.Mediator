namespace Vortex.Mediator;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MediatorBindingAttribute : Attribute
{
    public MediatorBindingAttribute(Type bindingType)
    {
        BindingType = bindingType;
    }

    public Type BindingType { get; }
}
