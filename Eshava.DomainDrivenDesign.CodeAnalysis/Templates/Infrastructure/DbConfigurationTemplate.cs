using System.Collections.Generic;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class DbConfigurationTemplate
	{
		public static string GetDbConfiguration(InfrastructureModel model, string databaseSchema, string fullQualifiedDomainNamespace, bool addAssemblyCommentToFiles)
		{
			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";
			var classDeclaration = $"{model.Name}DbConfiguration";
			var baseType = "IEntityTypeConfiguration".AsGeneric(model.Name).ToSimpleBaseType();

			var unitInformation = new UnitInformation(classDeclaration, @namespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword);
			unitInformation.AddBaseType(baseType);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.MetaData.BUILDERS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.MetaData.INTERFACES);


			var parameterDeclaration = "builder".ToParameter()
					.WithType("EntityTypeBuilder".AsGeneric(model.Name));

			var methodDeclarationName = "Configure";
			var methodDeclaration = methodDeclarationName
				.ToMethod(Eshava.CodeAnalysis.SyntaxConstants.Void, CreateBuilderStatements(model.TableName, databaseSchema, model.IdentifierGenerationOnAdd), SyntaxKind.PublicKeyword)
				.WithParameter(parameterDeclaration);

			unitInformation.AddMethod((methodDeclarationName, methodDeclaration));

			return unitInformation.CreateCodeString();

		}

		private static StatementSyntax[] CreateBuilderStatements(string tableName, string databaseSchema, bool identifierGenerationOnAdd)
		{
			var statements = new List<StatementSyntax>
			{
				CreateBuilderStatement("builder", "HasKey", "p".ToPropertyExpression("Id")).ToExpressionStatement(),
			};

			if (identifierGenerationOnAdd)
			{
				statements.Add(
					CreateBuilderStatement("builder", "Property", "p".ToPropertyExpression("Id"))
					.Call("ValueGeneratedOnAdd")
					.ToExpressionStatement()
				);
			}

			statements.Add("builder"
				.Call("ToTable", false, tableName.ToLiteralArgument(), databaseSchema.ToLiteralArgument())
				.ToExpressionStatement()
			);

			return statements.ToArray();
		}

		private static InvocationExpressionSyntax CreateBuilderStatement(string variable, string method, SimpleLambdaExpressionSyntax expression)
		{
			return variable.Call(method, false, expression.ToArgument());
		}
	}
}