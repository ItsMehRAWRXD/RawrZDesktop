using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace RawrZDesktop
{
    class TestProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing RawrZ IDE Core Functionality...\n");
            IDETestConsole.RunAllTests();
            Console.WriteLine("\nIDE functionality test complete!");
        }
    }
}