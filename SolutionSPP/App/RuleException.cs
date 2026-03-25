using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App
{
    public class RuleException : Exception
    {
        public RuleException(string message) : base(message) { }
    }
}