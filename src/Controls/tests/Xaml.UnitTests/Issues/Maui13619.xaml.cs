using System;
using System.Linq;
using System.Reflection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Core.UnitTests;
using Microsoft.Maui.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace Microsoft.Maui.Controls.Xaml.UnitTests
{


	public partial class Maui13619 : ContentPage
	{
		public Maui13619() => InitializeComponent();
		public Maui13619(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		[TestFixture]
		class Tests
		{
			[SetUp] public void Setup() => AppInfo.SetCurrent(new MockAppInfo());
			[TearDown] public void TearDown() => AppInfo.SetCurrent(null);

			[Test]
			public void AppThemeBindingAndDynamicResource([Values(false, true)] bool useCompiledXaml)
			{
				var page = new Maui13619(useCompiledXaml);
				Assert.That(page.label0.TextColor, Is.EqualTo(Colors.HotPink));
				Assert.That(page.label0.BackgroundColor, Is.EqualTo(Colors.DarkGray));

				page.Resources["Primary"] = Colors.SlateGray;
				Assert.That(page.label0.BackgroundColor, Is.EqualTo(Colors.SlateGray));

			}

			[Test]
			public void AppThemeBindingIsOptimized()
			{
				MockCompiler.Compile(typeof(Maui13619), out var methodDef);
				Assert.That(!methodDef.Body.Instructions.Any(instr => ContainsRuntimeReflectionExtensions(methodDef, instr)), "This Xaml still generates RuntimeReflectionExtensions calls)");
			}

			bool ContainsRuntimeReflectionExtensions(MethodDefinition methodDef, Mono.Cecil.Cil.Instruction instruction)
			{
				if (instruction.OpCode != OpCodes.Call)
					return false;
				if (!(instruction.Operand is MethodReference methodRef))
					return false;
				if (!Build.Tasks.TypeRefComparer.Default.Equals(methodRef.DeclaringType, methodDef.Module.ImportReference(typeof(RuntimeReflectionExtensions))))
					return false;
				return true;
			}
		}
	}
}
