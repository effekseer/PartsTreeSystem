using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartsTreeSystem
{
	public class NodeTreeAssetEditorProperty
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
		NodeTreeAsset nodeTreeAsset;
		Environment environment;

		public IReadOnlyList<NodeProperty> Properties { get { return nodeProperties; } }

		public NodeTreeAssetEditorProperty(NodeTreeAsset nodeTreeAsset, Environment environment)
		{
			this.nodeTreeAsset = nodeTreeAsset;
			this.environment = environment;
			Rebuild();
		}


		public void Rebuild()
		{
			nodeProperties.Clear();

			foreach (var nodeBase in nodeTreeAsset.InternalData.Bases)
			{
				object generator = null;

				if (!string.IsNullOrEmpty(nodeBase.BaseType))
				{
					generator = environment.GetType(nodeBase.BaseType);
				}
				else if (!string.IsNullOrEmpty(nodeBase.Template))
				{
					var path = environment.GetAbsolutePath(environment.GetAssetPath(nodeTreeAsset), nodeBase.Template);
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