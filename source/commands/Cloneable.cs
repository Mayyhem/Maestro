using Maestro;
using System.Reflection;

public abstract class Cloneable : ICloneable<Cloneable>
{
    // Allows for cloning of object properties via reflection
    public void CloneTo<T>(Cloneable cloneable)
    {
        PropertyInfo[] sourceProperties = GetType().GetProperties();
        PropertyInfo[] targetProperties = typeof(T).GetProperties();

        foreach (var sourceProperty in sourceProperties)
        {
            foreach (var targetProperty in targetProperties)
            {
                if (targetProperty.Name == sourceProperty.Name &&
                    targetProperty.PropertyType == sourceProperty.PropertyType &&
                    targetProperty.CanWrite)
                {
                    targetProperty.SetValue(cloneable, sourceProperty.GetValue(this));
                    break;
                }
            }
        }

        return;
    }
}