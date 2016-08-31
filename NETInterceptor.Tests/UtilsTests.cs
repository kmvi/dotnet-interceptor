using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETInterceptor.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void test1()
        {
            var ptr = typeof(Action).GetMethod("BeginInvoke").MethodHandle.GetFunctionPointer();
            Precode.Create(ptr);
        }
    }
}
