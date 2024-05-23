using System.Collections.Generic;

namespace Maestro
{
    public interface IDatabaseHandler
    {
        bool Exists<T>(string propertyName, object value) where T : class;
        List<T> GetAll<T>() where T : class, new();
        List<T> GetByProperty<T>(string propertyName, object value) where T : class, new();
        void Upsert<T>(T item) where T : class;
    }
}