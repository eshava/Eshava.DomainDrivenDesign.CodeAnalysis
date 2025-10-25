using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public class UnitInformation
	{
		private List<(string FieldName, FieldDeclarationSyntax Declaration)> _fields = new List<(string FieldName, FieldDeclarationSyntax Declaration)>();
		private List<(string FieldName, StatementSyntax Statement)> _constructorBodyStatements = new List<(string FieldName, StatementSyntax Statement)>();
		private List<(string Name, ArgumentSyntax Argument)> _constructorArguments = new List<(string Name, ArgumentSyntax Argument)>();
		private List<(string Name, PropertyDeclarationSyntax Property)> _properties = new List<(string Name, PropertyDeclarationSyntax Property)>();
		private List<(string Name, MemberDeclarationSyntax Method)> _methods = new List<(string Name, MemberDeclarationSyntax Method)>();
		private List<NameAndType> _constructorParameters = new List<NameAndType>();
		private readonly bool _addAssemblyComment;

		public UnitInformation(string className = null, string @namespace = null, bool isInterface = false, bool addConstructor = true, bool isEnumeration = false, bool addAssemblyComment = false)
		{
			ClassName = className;
			Namespace = @namespace;
			IsInterface = isInterface;
			HasConstructor = addConstructor;
			Usings = new HashSet<string>();
			BaseTypes = new List<SimpleBaseTypeSyntax>();
			Attributes = new List<AttributeSyntax>();
			ClassModifiers = new List<SyntaxKind>();
			ConstructorModifiers = new List<SyntaxKind>();
			EnumerationMembers = new List<EnumMemberDeclarationSyntax>();

			IsEnumeration = isEnumeration;
			_addAssemblyComment = addAssemblyComment;
			if (IsEnumeration)
			{
				IsInterface = false;
				HasConstructor = false;
			}
		}

		public string ClassName { get; }
		public string Namespace { get; }
		public bool IsInterface { get; }
		public bool IsEnumeration { get; }
		public bool HasConstructor { get; }
		public HashSet<string> Usings { get; set; }
		public IEnumerable<FieldDeclarationSyntax> Fields => _fields.OrderBy(field => field.FieldName).Select(field => field.Declaration).ToList();
		public IEnumerable<NameAndType> ConstructorParameters => _constructorParameters;
		public IEnumerable<ArgumentSyntax> ConstructorArguments => _constructorArguments.Select(argument => argument.Argument).ToList();
		public IEnumerable<StatementSyntax> ConstructorBodyStatements => _constructorBodyStatements.Select(statement => statement.Statement).ToList();
		public IEnumerable<MemberDeclarationSyntax> Methods => _methods.Select(method => method.Method).ToList();
		public List<AttributeSyntax> Attributes { get; set; }
		public IEnumerable<PropertyDeclarationSyntax> Properties => _properties.Select(property => property.Property).ToList();
		public IList<SimpleBaseTypeSyntax> BaseTypes { get; set; }
		public IList<EnumMemberDeclarationSyntax> EnumerationMembers { get; set; }
		public IList<SyntaxKind> ClassModifiers { get; set; }
		public IList<SyntaxKind> ConstructorModifiers { get; set; }

		public void AddUsing(string @using)
		{
			if (@using == Namespace)
			{
				return;
			}

			if (!@using.IsNullOrEmpty() && !Usings.Contains(@using))
			{
				Usings.Add(@using);
			}
		}

		public void AddClassModifier(params SyntaxKind[] modifiers)
		{
			foreach (var modifier in modifiers)
			{
				ClassModifiers.Add(modifier);
			}
		}

		public void AddContructorModifier(params SyntaxKind[] modifiers)
		{
			foreach (var modifier in modifiers)
			{
				ConstructorModifiers.Add(modifier);
			}
		}

		public void AddBaseType(params SimpleBaseTypeSyntax[] types)
		{
			foreach (var type in types)
			{
				BaseTypes.Add(type);
			}
		}

		public bool AddMethod((string Name, MemberDeclarationSyntax Method) method)
		{
			if (method.Method is null)
			{
				return false;
			}

			var existingMethod = _methods.FirstOrDefault(f => f.Name == method.Name);
			if (existingMethod.Method is not null && existingMethod.Method.IsEquivalentTo(method.Method))
			{
				return false;
			}

			_methods.Add(method);

			return true;
		}

		public bool AddMethod(MemberDeclarationSyntax method, string methodName)
		{
			return AddMethod((methodName, method));
		}

		public bool AddProperty(PropertyDeclarationSyntax property, string propertyName)
		{
			if (_properties.Any(f => f.Name == propertyName))
			{
				return false;
			}

			_properties.Add((propertyName, property));

			return true;
		}

		public bool AddField(string fieldName, FieldDeclarationSyntax declaration)
		{
			return AddField((fieldName, declaration));
		}

		public bool AddField((string FieldName, FieldDeclarationSyntax Declaration) field)
		{
			if (_fields.Any(f => f.FieldName == field.FieldName))
			{
				return false;
			}

			_fields.Add(field);

			return true;
		}

		public void AddLogger(string className, bool asArgument = false)
		{
			Usings.Add(CommonNames.Namespaces.LOGGING);

			var nameAndType = new NameAndType("logger", "ILogger".AsGeneric(className));
			AddConstructorParameter(nameAndType);

			if (asArgument)
			{
				AddConstructorArgument("logger");
			}
			else
			{
				AddConstructorBodyStatementAndField(nameAndType);
			}
		}

		public void AddScopedSettings(string scopedSettingsUsing, string scopedSettingsClass, bool asArgument = false)
		{
			if (!scopedSettingsUsing.IsNullOrEmpty())
			{
				Usings.Add(scopedSettingsUsing);
			}

			var nameAndType = new NameAndType("scopedSettings", scopedSettingsClass.ToIdentifierName());
			AddConstructorParameter(nameAndType);

			if (asArgument)
			{
				AddConstructorArgument("scopedSettings");
			}
			else
			{
				AddConstructorBodyStatementAndField(nameAndType);
			}
		}

		public void AddValidationRuleEngine(bool addValidationRuleEngine)
		{
			if (addValidationRuleEngine)
			{
				AddUsing(CommonNames.Namespaces.Eshava.Core.VALIDATION.INTERFACES);
				AddConstructorParameter(ApplicationNames.Engines.ValidationRule);
				AddConstructorArgument(ApplicationNames.Engines.VALIDATIONRULE);
			}
			else
			{
				AddConstructorArgument(nameof(Eshava.CodeAnalysis.SyntaxConstants.Null), Eshava.CodeAnalysis.SyntaxConstants.Null);
			}
		}

		public void AddSortQueryEngine(bool asArgument = false)
		{
			AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			AddConstructorParameter(ApplicationNames.Engines.Sorting);

			if (asArgument)
			{
				AddConstructorArgument(ApplicationNames.Engines.SORTING);
			}
			else
			{
				AddConstructorBodyStatementAndField(ApplicationNames.Engines.Sorting);
			}
		}

		public void AddWhereQueryEngine(bool asArgument = false)
		{
			AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			AddConstructorParameter(ApplicationNames.Engines.Where);

			if (asArgument)
			{
				AddConstructorArgument(ApplicationNames.Engines.WHERE);
			}
			else
			{
				AddConstructorBodyStatementAndField(ApplicationNames.Engines.Where);
			}
		}

		public void AddWhereAndSortQueryEngine()
		{
			AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			AddConstructorParameter(ApplicationNames.Engines.Where);
			AddConstructorParameter(ApplicationNames.Engines.Sorting);
			AddConstructorArgument(ApplicationNames.Engines.WHERE);
			AddConstructorArgument(ApplicationNames.Engines.SORTING);
		}

		public void AddValidationEngine(bool asArgument = false)
		{
			AddUsing(CommonNames.Namespaces.Eshava.Core.VALIDATION.INTERFACES);
			AddConstructorParameter(DomainNames.VALIDATION.Parameter);

			if (asArgument)
			{
				AddConstructorArgument(DomainNames.VALIDATION.ENGINE);
			}
			else
			{
				AddConstructorBodyStatementAndField(DomainNames.VALIDATION.Parameter);

			}
		}

		public void AddConstructorParameter(NameAndType nameAndType, ParameterTargetType targetType = ParameterTargetType.Field)
		{
			if (ConstructorParameters.Any(p => p.Name == nameAndType.Name))
			{
				return;
			}

			AddConstructorParameter(nameAndType);
			switch (targetType)
			{
				case ParameterTargetType.Field:
					AddConstructorBodyStatementAndField(nameAndType);

					break;
				case ParameterTargetType.Argument:
					AddConstructorArgument(nameAndType.Name);

					break;
				case ParameterTargetType.Property:
					AddConstructorBodyStatementAndProperty(nameAndType, false);

					break;
				case ParameterTargetType.PropertyReadonly:
					AddConstructorBodyStatementAndProperty(nameAndType, true);

					break;
			}
		}

		public void AddConstructorParameter(string name, TypeSyntax type, ParameterTargetType targetType = ParameterTargetType.Field)
		{
			AddConstructorParameter(new NameAndType(name, type), targetType);
		}

		public void AddConstructorParameter(string name, string type, ParameterTargetType targetType = ParameterTargetType.Field)
		{
			AddConstructorParameter(new NameAndType(name, type.ToType()), targetType);
		}

		public void AddConstructorBodyStatementAndField(string parameterName, TypeSyntax type)
		{
			AddConstructorBodyStatementAndField(new NameAndType(parameterName, type));
		}

		public void AddConstructorBodyStatementAndField(NameAndType nameAndType)
		{
			var fieldName = $"_{nameAndType.Name}";

			AddField(fieldName, fieldName.ToReadonlyField(nameAndType.Type));

			if (!_constructorBodyStatements.Any(statement => statement.FieldName == fieldName))
			{
				_constructorBodyStatements.Add((fieldName, fieldName.ToIdentifierName().Assign(nameAndType.Name.ToIdentifierName()).ToExpressionStatement()));
			}
		}

		public void AddConstructorBodyStatementAndProperty(NameAndType nameAndType, bool onlyGetter)
		{
			var propertyName = nameAndType.Name.ToPropertyName();

			AddProperty(propertyName.ToProperty(nameAndType.Type, SyntaxKind.PublicKeyword, true, !onlyGetter), propertyName);

			if (!_constructorBodyStatements.Any(statement => statement.FieldName == propertyName))
			{
				_constructorBodyStatements.Add((propertyName, propertyName.ToIdentifierName().Assign(nameAndType.Name.ToIdentifierName()).ToExpressionStatement()));
			}
		}

		public void AddConstructorBodyStatement(StatementSyntax statement)
		{
			_constructorBodyStatements.Add(("_", statement));
		}

		public void AddConstructorArgument(string name)
		{
			if (_constructorArguments.Any(argument => argument.Name == name))
			{
				return;
			}

			_constructorArguments.Add((name, name.ToArgument()));
		}

		public void AddConstructorArgument(string name, LiteralExpressionSyntax literalExpressionSyntax)
		{
			if (_constructorArguments.Any(argument => argument.Name == name))
			{
				return;
			}

			_constructorArguments.Add((name, literalExpressionSyntax.ToArgument()));
		}

		public void AddAttribute(AttributeSyntax attribute)
		{
			Attributes.Add(attribute);
		}

		public void AddAttributes(IEnumerable<AttributeSyntax> attributes)
		{
			Attributes.AddRange(attributes);
		}

		public CompilationUnitSyntax CreateCompilation()
		{
			if (IsEnumeration)
			{
				return CreateEnumerationCompilation();
			}

			return IsInterface
				? CreateInterfaceCompilation()
				: CreateClassCompilation();
		}

		public string CreateCodeString()
		{
			return CreateCompilation()
				.NormalizeWhitespace()
				.ToFullString();
		}

		private void AddConstructorParameter(NameAndType nameAndType)
		{
			if (_constructorParameters.Any(parameter => parameter.Name == nameAndType.Name))
			{
				return;
			}

			_constructorParameters.Add(nameAndType);
		}

		private CompilationUnitSyntax CreateEnumerationCompilation()
		{
			var compilationUnit = SyntaxHelper.CreateCompilationUnit();
			var @namespace = Namespace.ToNamespace();

			var enumerationDeclaration = ClassName.ToEnumeration(EnumerationMembers, ClassModifiers.ToArray());

			@namespace = @namespace.AddMembers(enumerationDeclaration);
			compilationUnit = compilationUnit.AddMembers(@namespace);

			return AddGlobalComment(compilationUnit);
		}

		private CompilationUnitSyntax CreateClassCompilation()
		{
			var compilationUnit = SyntaxHelper.CreateCompilationUnit();
			compilationUnit = compilationUnit.AddUsings(Usings);

			var @namespace = Namespace.ToNamespace();
			var classDeclaration = ClassName.ToClass(Attributes, ClassModifiers.ToArray());

			if (BaseTypes.Count > 0)
			{
				classDeclaration = classDeclaration.AddBaseListTypes(BaseTypes.ToArray());
			}
			classDeclaration = classDeclaration.AddMembers(Fields.ToArray());

			if (HasConstructor)
			{
				var constructorDeclaration = ClassName.ToContructor(
					ConstructorParameters,
					ConstructorBodyStatements,
					ConstructorModifiers.ToArray()
				);

				if (ConstructorArguments.Any())
				{
					constructorDeclaration = constructorDeclaration.WithInitializer(ConstructorArguments.ToArray());
				}

				classDeclaration = classDeclaration.AddMembers(constructorDeclaration);
			}

			classDeclaration = classDeclaration.AddMembers(Properties.ToArray());
			classDeclaration = classDeclaration.AddMembers(Methods.ToArray());

			@namespace = @namespace.AddMembers(classDeclaration);
			compilationUnit = compilationUnit.AddMembers(@namespace);

			return AddGlobalComment(compilationUnit);
		}

		private CompilationUnitSyntax CreateInterfaceCompilation()
		{
			var compilationUnit = SyntaxHelper.CreateCompilationUnit();

			compilationUnit = compilationUnit.AddUsings(Usings);

			var @namespace = Namespace.ToNamespace();
			var interfaceDeclaration = ClassName.ToInterface(Attributes, ClassModifiers.ToArray());

			if (BaseTypes.Count > 0)
			{
				interfaceDeclaration = interfaceDeclaration.AddBaseListTypes(BaseTypes.ToArray());
			}

			interfaceDeclaration = interfaceDeclaration.AddMembers(Methods.ToArray());

			@namespace = @namespace.AddMembers(interfaceDeclaration);
			compilationUnit = compilationUnit.AddMembers(@namespace);

			return AddGlobalComment(compilationUnit);
		}

		private CompilationUnitSyntax AddGlobalComment(CompilationUnitSyntax compilationUnit)
		{
			if (!_addAssemblyComment)
			{
				return compilationUnit;
			}

			var gobalComment = SyntaxFactory.Comment($"/*\n * Generator Version: {typeof(UnitInformation).Assembly.GetName().Version}\n*/");

			return compilationUnit.WithLeadingTrivia(SyntaxFactory.TriviaList(gobalComment));
		}
	}
}