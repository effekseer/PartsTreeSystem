using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PartsTreeSystem
{
	/// <summary>
	/// Interface for instances to be saved or edited
	/// </summary>
	public interface IInstance
	{
		/// <summary>
		/// Unique ID
		/// </summary>
		int InstanceID { get; set; }
	}

	/// <summary>
	/// Interface for instances to contain instances
	/// </summary>
	public interface IInstanceContainer
	{
		/// <summary>
		/// Returns an instance with a matching ID from among the instances stored.
		/// </summary>
		/// <param name="instanceID">Instance ID</param>
		/// <returns></returns>
		IInstance FindInstance(int instanceID);
	}
}