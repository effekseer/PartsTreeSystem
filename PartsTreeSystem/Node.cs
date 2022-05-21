using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	public interface IInstanceID
	{
		int InstanceID { get; set; }
	}

	public interface IAssetInstanceRoot
	{
		IInstanceID FindInstance(int id);
	}

	public interface INode : IInstanceID
	{
		void AddChild(INode node);

		void RemoveChild(int instanceID);

		void InsertChild(int index, INode node);

		IReadOnlyCollection<INode> GetChildren();
	}
}