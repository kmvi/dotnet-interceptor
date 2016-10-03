using NETInterceptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Examples
{
    public class ConstructorExample
    {
        private static HookHandle _handle;

        public static void Demo()
        {
            var target = typeof(Random).GetConstructor(new[] { typeof(int) });
            var subst = typeof(ConstructorExample).GetMethod("CtorSubst");

            using (_handle = Intercept.On(target, subst)) {
                var rnd = new Random(4);
                var num = rnd.Next(); // returns 0
                Console.WriteLine();
            }
        }

        private int[] SeedArray;

        public void CtorSubst(int seed)
        {
            Console.WriteLine("--- Constructor interception --- ");
            Console.WriteLine("--- Random.ctor(int seed) ---");

            // this line creates array in Random instance
            SeedArray = new int[56];
        }
    }
}
