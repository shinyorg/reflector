using System;
using Shiny.Reflector;

namespace TestSample
{
    [Reflector]
    public partial class MySampleClass
    {
        public int Id { get; private set; }
        public int Age { get; set; }
        public string Name { get; set; }
    }
}
