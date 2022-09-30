using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public class NodeTree : IInstanceContainer
	{
		public INode Root;

		public IInstance FindInstance(int id)
		{
			return FindInstance(Root, id);
		}

		public INode FindParent(int id)
		{
			return FindParent(Root, id);
		}

		IInstance FindInstance(INode node, int id)
		{
			if (node.InstanceID == id)
			{
				return node;
			}

			foreach (var child in node.GetChildren())
			{
				var result = FindInstance(child, id);
				if (result != null)
				{
					return result;
				}
			}

			return null;

		}

		INode FindParent(INode parent, int id)
		{
			if (parent.GetChildren().Any(_ => _.InstanceID == id))
			{
				return parent;
			}

			foreach (var child in parent.GetChildren())
			{
				var result = FindParent(child, id);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}
	}

	class NodeTreeBase
	{
		public string BaseType;

		public string Template;

		public Dictionary<int, int> IDRemapper = new Dictionary<int, int>();

		public Dictionary<int, Difference> Differences = new Dictionary<int, Difference>();

		public int ParentID;
	}
}