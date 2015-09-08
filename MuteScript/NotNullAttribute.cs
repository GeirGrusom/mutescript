using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuteScript
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple =false, Inherited =true)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
