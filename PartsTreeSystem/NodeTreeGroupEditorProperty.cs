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
		}

		List<NodeProperty> nodeProperties = new List<NodeProperty>();

		public IReadOnlyList<NodeProperty> Properties { get { return nodeProperties; } }

		NodeTreeGroup nodeTreeGroup;
		Environment env;

		public NodeTreeGroupEditorProperty(NodeTreeGroup nodeTreeGroup, Environment env)
		{
			this.nodeTreeGroup = nodeTreeGroup;
			this.env = env;
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
					generator = env.GetType(nodeBase.BaseType);
				}
				else if (!string.IsNullOrEmpty(nodeBase.Template))
				{
					var path = Utility.GetAbsolutePath(env.GetAssetPath(nodeTreeGroup), nodeBase.Template);
					generator = env.GetAsset(path);
				}

				foreach (var remapper in nodeBase.IDRemapper)
				{
					nodeProperties.Add(new NodeProperty { Generator = generator, InstanceID = remapper.Value });
				}
			}
		}
	}
}