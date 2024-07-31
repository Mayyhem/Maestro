namespace Maestro
{
    // Allows for cloning of object properties via reflection
    public interface ICloneable<T>
    {
        void CloneTo<T>(Cloneable cloneable);
    }
}
