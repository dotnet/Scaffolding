using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelTypesLocatorTestClassLibrary
{
    public class Car
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ManufacturerID { get; set; }
        public Manufacturer Manufacturer { get; set; }
    }

    public class Manufacturer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Car> Cars { get; set; }
    }
}
