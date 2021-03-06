using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartsTreeSystem
{
	public class NodeTreeGroupEditorProperty
	{
		public class NodeProperty
		{
			public int InstanceID;
			public object Generator;

			/// <summary>
			/// Get whether the value of a field has changed from the default
			/// </summary>
			/// <param name="pathChains">e.g. prop.Base.Template -> new [] {prop, Base, Template}</param>
			/// <returns></returns>
			public bool IsValueEdited(IEnumerable<string> pathChains)
			{
				if (Base.Differences.TryGetValue(InstanceID, out var value))
				{
					var akg = new AccessKeyGroup { Keys = pathChains.Select(_ => new AccessKey { Name = _ }).ToArray() };
					return value.ContainTarget(akg);
				}

				return false;
			}

			internal NodeTreeBase Base;
		}

		List<NodeProperty> nodeProperties = new List<NodeProperty>();
		NodeTreeGroup nodeTreeGroup;
		Environment environment;

		public IReadOnlyList<NodeProperty> Properties { get { return nodeProperties; } }

		public NodeTreeGroupEditorProperty(NodeTreeGroup nodeTreeGroup, Environment environment)
		{
			this.nodeTreeGroup = nodeTreeGroup;
			this.environment = environment;
			Rebuild();
		}


		public void Rebuild()
		{
			nodeProperties.Clear();

			foreach (var nodeBase in nodeTreeGroup.InternalData.Bases)
			{
				object generator = null;

				if (!string.IsNullOrEmpty(nodeBase.BaseType))
				{
					generator = environment.GetType(nodeBase.BaseType);
				}
				else if (!string.IsNullOrEmpty(nodeBase.Template))
				{
					var path = Utility.GetAbsolutePath(environment.GetAssetPath(nodeTreeGroup), nodeBase.Template);
					generator = environment.GetAsset(path);
				}

				foreach (var remapper in nodeBase.IDRemapper)
				{
					nodeProperties.Add(new NodeProperty { Generator = generator, InstanceID = remapper.Value, Base = nodeBase });
				}
			}
		}
	}
}