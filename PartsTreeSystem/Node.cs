using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	/// <summary>
	/// Special interface for a node in a tree structure
	/// </summary>
	public interface INode : IInstance
	{
		void AddChild(INode node);

		void RemoveChild(int instanceID);

		void InsertChild(int index, INode node);

		IReadOnlyCollection<INode> GetChildren();
	}
}