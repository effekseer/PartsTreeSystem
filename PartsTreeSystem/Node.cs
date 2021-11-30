using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public interface IInstanceID
	{
		public int InstanceID { get; set; }
	}

	public interface IAssetInstanceRoot
	{
		public IInstanceID? FindInstance(int id);
	}

	public interface INode : IInstanceID
	{
		public void AddChild(INode node);

		public void RemoveChild(int instanceID);

		public IReadOnlyCollection<INode> GetChildren();
	}
}