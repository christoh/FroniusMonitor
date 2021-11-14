using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hochstaetter.Fronius.Contracts
{
    public interface IHierarchicalCollection:IHaveDisplayName
    {
        IEnumerable ItemsEnumerable { get; }
        IEnumerable ChildrenEnumerable { get; }
    }
}
