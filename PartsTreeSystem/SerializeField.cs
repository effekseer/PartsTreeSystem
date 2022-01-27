using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartsTreeSystem
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SerializeField : Attribute { }
}