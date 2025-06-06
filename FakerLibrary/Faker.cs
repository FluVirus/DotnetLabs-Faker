﻿using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using University.DotnetLabs.Lab2.FakerLibrary.DefualtGenerators;
using University.DotnetLabs.Lab2.FakerLibrary.Exceptions;

namespace University.DotnetLabs.Lab2.FakerLibrary;

public class Faker : IFaker
{
    public Dictionary<Type, Generator> Generators { get; } = new();
    protected MethodInfo _createMethodInfo = typeof(Faker).GetMethod("Create", new Type[0]);
    protected FakerConfig? _config;
    public Type[]? CurrentGenerics { get; internal set; }
    public Type[]? CurrentGenericDeclaration { get; internal set; }

    public ICollection<Exception> Exceptions { get; } = new LinkedList<Exception>();

    public object? Create(Type createdType) 
    {
        object createdObject;
        //------------------------------------------------If generator of T is registered--------------------------------
        bool result = Generators.TryGetValue((createdType.IsGenericType)?createdType.GetGenericTypeDefinition():createdType, out Generator? generator);
        CurrentGenerics = createdType.GetGenericArguments();
        if (result)
        {
            if (generator == null)
            {
                return default;
            }
            else
            {
                return generator.Generate();
            }
        }
        //-------------------------------------------------If generator of t isn't registered-----------------------------
        else
        {
            //------------------------------------------------Start constructor----------------------------------------------
            ConstructorInfo[] constructors = createdType.GetConstructors();
            IEnumerable<ConstructorInfo> publicConstructors = from constructor in constructors where constructor.IsPublic && !constructor.IsAbstract select constructor;

            if (publicConstructors.Count() == 0)
            {
                throw new NotInstanceableException($"Class {createdType.FullName} declares no public constructors");
            }
            Random random = new Random();
            ConstructorInfo selectedConstructor = publicConstructors.ElementAt<ConstructorInfo>(
                Index.FromStart(random.Next(publicConstructors.Count() - 1)));
            ParameterInfo[] parametersInfo = selectedConstructor.GetParameters();
            object[] parameters = new object[parametersInfo.Length];
            for (int i = 0; i < parametersInfo.Length; i++)
            {
                ParameterInfo parameterInfo = parametersInfo[i];
                Type parameterType = parameterInfo.ParameterType;
                CurrentGenerics = parameterType.GetGenericArguments();
                if (parameterInfo.HasDefaultValue)
                {
                    parameters[i] = parameterInfo.DefaultValue;
                }
                else
                {
                    //configs first
                    if (_config != null) 
                    {
                        result = _config.Generators.TryGetValue(createdType.FullName + "." + parameterInfo.Name.ToLower(), out generator);
                    }
                    if (!result || generator == null)
                    {
                        result = Generators.TryGetValue(parameterType, out generator);
                    }
                    if (result && generator != null)
                    {
                        parameters[i] = generator.Generate();
                    }
                    else
                    {
                        if (parameterType.IsValueType)
                        {
                            parameters[i] = Activator.CreateInstance(parameterType);
                        }
                        else if (parameterType.IsEnum)
                        {
                            parameters[i] = default;
                        }
                        else
                        {
                            parameters[i] = null;
                        }
                    }
                }

            }
            //-----------------------------------------------------End constructor----------------------------------------------
            createdObject = selectedConstructor.Invoke(parameters);

            if (createdType.IsClass)
            {
                Generator referenceReturner = new ReferenceReturner(null, createdType);
                Generators.Remove(createdType);
                Generators.Add(createdType, referenceReturner);
            }
            try
            {
                //------------------------------------------------Start Fields & props----------------------------------------------
                FieldInfo[] publicFields = createdType.GetFields();
                IEnumerable<FieldInfo> nonReadOnlyFields = from publicField in publicFields where !publicField.IsInitOnly && !publicField.IsStatic select publicField;
                PropertyInfo[] publicProperties = createdType.GetProperties();
                IEnumerable<PropertyInfo> writeableProperties = from publicProperty in publicProperties where publicProperty.CanWrite && (publicProperty.SetMethod != null) select publicProperty;
                //-----------------------------------Fields------------------------------------    
                foreach (FieldInfo fieldInfo in nonReadOnlyFields)
                {
                    Type fieldType = fieldInfo.FieldType;
                    CurrentGenerics = fieldType.GetGenericArguments();
                    if (_config != null)
                    {
                        result = _config.Generators.TryGetValue(createdType.FullName + "." + fieldInfo.Name.ToLower(), out generator);
                    }
                    if (!result || generator == null)
                    {
                        result = Generators.TryGetValue(fieldType, out generator);
                    }
                    if (result)
                    {
                        if (generator == null)
                        {
                            fieldInfo.SetValue(createdObject, default);
                        }
                        else
                        {
                            fieldInfo.SetValue(createdObject, generator.Generate());
                        }
                    }
                    else
                    {
                        //try ro generate instance
                        MethodInfo recursiveMethod = _createMethodInfo.MakeGenericMethod(fieldType);
                        object? createdField = null;
                        try
                        {
                            createdField = recursiveMethod.Invoke(this, null);
                            if (fieldType.IsClass)
                            {
                                Generator referenceReturner = new ReferenceReturner(null, fieldType);
                                Generators.Add(fieldType, referenceReturner);
                            }
                        }
                        catch (Exception ex)
                        {
                            createdField = default;
                            Exceptions.Add(ex);
                        }
                        finally
                        {
                            fieldInfo.SetValue(createdObject, createdField);
                            if (fieldType.IsClass)
                            {
                                Generators.Remove(fieldType);
                            }
                        }
                    }
                }
                //-----------------------------------Props------------------------------------    
                foreach (PropertyInfo propertyInfo in writeableProperties)
                {
                    Type propertyType = propertyInfo.PropertyType;
                    CurrentGenerics = propertyType.GetGenericArguments();
                    if (_config != null)
                    {
                        result = _config.Generators.TryGetValue(createdType.FullName + "." + propertyInfo.Name.ToLower(), out generator);
                    }
                    if (!result || generator == null)
                    {
                        result = Generators.TryGetValue(propertyType, out generator);
                    }
                    if (result)
                    {
                        if (generator == null)
                        {
                            propertyInfo.SetValue(createdObject, default);
                        }
                        else
                        {
                            propertyInfo.SetValue(createdObject, generator.Generate());
                        }
                    }
                    else
                    {
                        //try ro generate instance
                        MethodInfo recursiveMethod = _createMethodInfo.MakeGenericMethod(propertyType);
                        object? createdField = null;
                        try
                        {
                            createdField = recursiveMethod.Invoke(this, null);
                            if (propertyType.IsClass)
                            {
                                Generator referenceReturner = new ReferenceReturner(null, propertyType);
                                Generators.Add(propertyType, referenceReturner);
                            }
                        }
                        catch (Exception ex)
                        {
                            createdField = default;
                            Exceptions.Add(ex);
                        }
                        finally
                        {
                            propertyInfo.SetValue(createdObject, createdField);
                            if (propertyType.IsClass)
                            {
                                Generators.Remove(propertyType);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (createdType.IsClass)
                {
                    Generators.Remove(createdType);
                }
            }
            //------------------------------------------------End Fields & props----------------------------------------------
            //-----------------------------------End If generator isn't registered---------------------------------------------
        }
        return createdObject;
    }
    public T? Create<T>()
    {
        return (T?)Create(typeof(T));
    }

    public Faker(FakerConfig config) : this()
    {
        _config = config; 
    }
    public Faker()
    {
        Generator[] generators = {
            new BoolGenerator(),
            new ByteGenerator(),
            new CharGenerator(),
            new DecimalGenerator(),
            new DoubleGenerator(),
            new FloatGenerator(),
            new IntGenerator(),
            new LongGenerator(),
            new SByteGenerator(),
            new ShortGenerator(),
            new UIntGenerator(),
            new ULongGenerator(),
            new UShortGenerator(),
            new ListGenerator(this)
        };
        foreach (Generator generator in generators)
        {
            Generators.Add(generator.GeneratingType, generator);
        }
    }
}
