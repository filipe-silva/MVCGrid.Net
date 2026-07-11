using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Models
{
    /// <summary>
    /// A single named rendering-engine registration (a unique name + an
    /// assembly-qualified type string). Dependency-free replacement for
    /// System.Configuration.ProviderSettings, so the core has no dependency
    /// on System.Configuration.
    /// </summary>
    public class RenderingEngineSetting
    {
        public RenderingEngineSetting()
        {
        }

        public RenderingEngineSetting(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; set; }

        /// <summary>Assembly-qualified type name of the rendering engine.</summary>
        public string Type { get; set; }
    }

    /// <summary>
    /// Keyed collection of rendering-engine registrations. Dependency-free replacement
    /// for System.Configuration.ProviderSettingsCollection. The name indexer returns
    /// null when the name is not present, matching the behavior MVCGrid relies on.
    /// </summary>
    public class RenderingEngineCollection : IEnumerable<RenderingEngineSetting>
    {
        private readonly List<RenderingEngineSetting> _items = new List<RenderingEngineSetting>();

        public void Add(RenderingEngineSetting setting)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));

            // Replace any existing entry with the same name (case-insensitive).
            Remove(setting.Name);
            _items.Add(setting);
        }

        public void Add(string name, string type)
        {
            Add(new RenderingEngineSetting(name, type));
        }

        public void Remove(string name)
        {
            _items.RemoveAll(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public RenderingEngineSetting this[string name]
        {
            get { return _items.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)); }
        }

        public IEnumerator<RenderingEngineSetting> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
