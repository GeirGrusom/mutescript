# MuteScript
MuteScript parser/compiler

This is a scripting language based on the .NET CLR that is intended to be relatively safe, but fairly recognizable to C# or Java programmers.

## Basic syntax ##

One notable difference with most other languages is that there is no end-of-statement. Instead this happens when there is logical to end the statement.

    const i <- 100
    output!(i)

This syntax tree is a variable declaration with an assignment. The next statement however is a method invcation binary expression, which doesn't make sense if they were part of the same statement in that context.

## Modules ##

Unlike C# or Java, there are no namespacing, packages or static types. Instead you have modules.

    module MyModule

A module must have a name, and can have zero or more import statements followed by zero or more module members (methods, classes, fields).

## Class ##

A class is declared like this:

    (public|private|) (immutable|mutable) class ClassName[<template argument[, template argument...]>] [: Trait class [, Trait class]]
    {
    }
    
They can either be public or private. Private means they are private to the module. I.e. only members of the module can access it. If this is omitted the type is only available to the compile unit (i.e. the parsed file).
An immutable class can only have immutable members and pure methods (with some exceptions for caching functionality). Classes can also have constant members as long as the values assigned are immutable types.

MuteScript does not support inheritance. Instead it is intended to only support traits or mixins.
    
## Method ##

  (public|private|) [defer] [pure] MethodName([argument[, argument]]) : ReturnType ({ MethodBody }| => expression)
  
Methods marked `defer` cannot have a method body. These are methods that are considered implemented by the runtime.

Example:

  public pure ToString() : string => "MyToString"
  public pure ToString() : string { return "MyToString" }
  

## Fields ##

  (public|private) (const|mutable|immutable) FieldName (: DataType| <- Expression|: DataType <- Expression)

Note that there is no default values in MuteScript, even for mutable fields. All fields must be assigned.

Example:

  public const Length <- 100
  public immutable Length <- 100
  public const Length : int <- 100
  public mutable Length : int
  
## Expressions ##

TODO
