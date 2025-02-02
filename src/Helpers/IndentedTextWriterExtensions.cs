﻿using Microsoft.CodeAnalysis;
using System.CodeDom.Compiler;

namespace Minimal.Mvvm.SourceGenerator
{
    internal static class IndentedTextWriterExtensions
    {
        public static void WriteSourceHeader(this IndentedTextWriter writer,
            NullableContextOptions nullableContextOptions, string? containingNamespace)
        {
            writer.WriteLine($"""
                              // <auto-generated>
                              //     Auto-generated by Minimal.Mvvm.SourceGenerator {typeof(Generator).Assembly.GetName().Version}
                              // </auto-generated>
                              """);
            writer.WriteLine();

            if (nullableContextOptions != NullableContextOptions.Disable)
            {
                writer.WriteNullableContext(nullableContextOptions);
                writer.WriteLine();
                writer.WriteLine();
            }

            if (!string.IsNullOrEmpty(containingNamespace))//begin namespace
            {
                writer.WriteLine($"namespace {containingNamespace}");
                writer.WriteLine("{");
                writer.Indent++;
            }
        }

        public static void WriteSourceFinished(this IndentedTextWriter writer, string? containingNamespace)
        {
            if (!string.IsNullOrEmpty(containingNamespace))//end namespace
            {
                writer.Indent--;
                writer.WriteLine("}");
            }
        }

        public static void WriteAccessibility(this IndentedTextWriter writer, Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public:
                    writer.Write("public ");
                    break;
                case Accessibility.ProtectedOrInternal:
                    writer.Write("protected internal ");
                    break;
                case Accessibility.Internal:
                    writer.Write("internal ");
                    break;
                case Accessibility.Protected:
                    writer.Write("protected ");
                    break;
                case Accessibility.ProtectedAndInternal:
                    writer.Write("private protected ");
                    break;
                case Accessibility.Private:
                    writer.Write("private ");
                    break;
            }
        }

        public static void WriteNullableContext(this IndentedTextWriter writer, NullableContextOptions nullableContextOptions)
        {
            switch (nullableContextOptions)
            {
                case NullableContextOptions.Disable:
                    writer.Write("#nullable disable");
                    break;
                case NullableContextOptions.Warnings:
                    writer.Write("#nullable enable warnings");
                    break;
                case NullableContextOptions.Annotations:
                    writer.Write("#nullable enable annotations");
                    break;
                case NullableContextOptions.Enable:
                    writer.Write("#nullable enable");
                    break;
            }
        }
    }
}
