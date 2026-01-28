# Eshava.DomainDrivenDesign.CodeAnalysis
A Roslyn-based library that provides a source code generator for the Eshava.DomainDrivenDesign approach

## Introduction
Depending on the scope of the model to be mapped, applying the domain-driven design approach from the Eshava.DomainDrivenDesign package requires the creation of a large amount of boilerplate code. 
This boilerplate code runs through all layers and, if the approach is strictly adhered to, is difficult or impossible to avoid.
This library is designed to automate the generation of the necessary boilerplate code. To do this, the individual data models, domain models, application endpoints, and endpoints can be easily configured.
The code generators take care of the rest. Since the auto-generated code files cannot be changed, all generated classes are marked with "partial" in principle. This allows individual code to be inserted anywhere.
In addition, the possibility of hooking into the generated code by overwriting various virtual methods has been provided. 
For the "Application" and "Api" layers, the insertion of code snippets has also been provided (see ApiGenerator and ApplicationGenerator classes).

A "simple" sample API is provided for better understanding of the code generator usage.


## Setup and project configuration
The code generator can be integrated into any existing project solution, provided that it complies with the structure specified in the NuGet package “Eshava.DomainDrivenDesign”.
Each layer requires its own generator class. A new project must be added to the solution in order to deploy or execute the generators. The target framework must be netstandard2.0.
The name of the project can be chosen freely. In the example, the project is called “Eshava.Example.SourceGenerator”. Four generator classes must be created in the project solution. One generator class for each layer.
Currently, the generator package only supports one project per layer. The names of the generator classes can also be freely chosen. An extension class is also required. This initialized the context for the generator classes.

The structure of all generator classes is the same except for the use of json configuration files and the factory called. The actual creation of the code files is done using the factory classes provided in the NuGet package.
The generator classes are needed so that the code is generated at compile time and inserted into the projects. 
In principle, the factory classes can also be used without the generator classes. To do this, they can be executed manually and the returned code files (text) can be saved in the file system.
The generator project requires a few special settings so that it can be executed in the background. The generator project itself must then be linked to every other project.

The control of what the individual generator classes should generate is done via JSON configuration files. Each project requires a configuration file for the project and at least one configuration file for the content to be generated.
For more information, see the section "Configuration Files". The individual configuration files should be stored in a central location, as shown in the example. Since the individual configuration files are used across projects.
In addition, the configuration files must be linked as "Additional Files" in the individual projects. Each project requires a virtual "Activator" file. This must be configured in the project, but does not need to be physically present. 
This is used to control the generator classes. Basically, all generator classes would always be executed in all linked projects. To prevent this, the virtual "Activator" file is used as a start marker.


### Source generator project file configuration
```
<PropertyGroup>
	<TargetFramework>netstandard2.0</TargetFramework>
	<LangVersion>latest</LangVersion>
	<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
</PropertyGroup>

<ItemGroup>
	<PackageReference Include="Eshava.CodeAnalysis" Version="1.0.0">
		<!-- Without this setting, the Eshava.CodeAnalysis.dll cannot be found when executing Microsoft.CodeAnalysis.Generator -->
		<GeneratePathProperty>True</GeneratePathProperty>
	</PackageReference>
	<PackageReference Include="Eshava.DomainDrivenDesign.CodeAnalysis" Version="1.0.0">
		<!-- Without this setting, the Eshava.DomainDrivenDesign.CodeAnalysis.dll cannot be found when executing Microsoft.CodeAnalysis.Generator -->
		<GeneratePathProperty>True</GeneratePathProperty>
	</PackageReference>
	<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.0.0" PrivateAssets="all" />
	<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="4.14.0" PrivateAssets="all" />
	<PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.2" GeneratePathProperty="true" PrivateAssets="all" />
	<PackageReference Include="System.Text.Encodings.Web" Version="10.0.2" GeneratePathProperty="true" PrivateAssets="all" />
	<PackageReference Include="System.Text.Json" Version="10.0.2" GeneratePathProperty="true" PrivateAssets="all" />
</ItemGroup>

<PropertyGroup>
	<GetTargetPathDependsOn>$(GetTargetPathDependsOn);GetDependencyTargetPaths</GetTargetPathDependsOn>
</PropertyGroup>

<Target Name="GetDependencyTargetPaths">
	<ItemGroup>
		<TargetPathWithTargetPlatformMoniker Include="$(PKGSystem_Text_Json)\lib\netstandard2.0\System.Text.Json.dll" IncludeRuntimeDependency="false" />
		<TargetPathWithTargetPlatformMoniker Include="$(PKGSystem_Text_Encodings_Web)\lib\netstandard2.0\System.Text.Encodings.Web.dll" IncludeRuntimeDependency="false" />
		<TargetPathWithTargetPlatformMoniker Include="$(PKGMicrosoft_Bcl_AsyncInterfaces)\lib\netstandard2.0\Microsoft.Bcl.AsyncInterfaces.dll" IncludeRuntimeDependency="false" />
		<TargetPathWithTargetPlatformMoniker Include="$(PKGEshava_CodeAnalysis)\lib\netstandard2.0\Eshava.CodeAnalysis.dll" IncludeRuntimeDependency="false" />
		<TargetPathWithTargetPlatformMoniker Include="$(PKGEshava_DomainDrivenDesign_CodeAnalysis)\lib\netstandard2.0\Eshava.DomainDrivenDesign.CodeAnalysis.dll" IncludeRuntimeDependency="false" />
	</ItemGroup>
</Target>
```

### Linking the source generator project to other project
```
<ItemGroup>
	<ProjectReference Include="..\Eshava.Example.SourceGenerator\Eshava.Example.SourceGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Add configuration files to project
```
<ItemGroup>
	<AdditionalFiles Include="..\SourceGenerator\domain.activator" />
	<AdditionalFiles Include="..\SourceGenerator\domain.project.json" />
	<AdditionalFiles Include="..\SourceGenerator\domain.models.ordering.json" />
	<AdditionalFiles Include="..\SourceGenerator\domain.models.organizations.json" />
</ItemGroup>
```


## Configuration Files
The configuration files contain instructions for the generators regarding which code files are to be generated. All configuration files are in JSON format. 
The names of the configuration files can also be freely chosen, as they must be added manually in the SourceGenerator project.
There are two basic types of configuration files: project and content.
The project configuration files contain general information about the project, such as the project namespace. The project files for each layer differ in their properties.
The configuration files for the project content differ significantly in each layer. For larger projects, it is recommended to divide the files by domain for clarity. 
The individual configuration files for each layer are automatically combined before the code generators are executed.


### Structure of the configuration files (project content)

Domain
```
{
	"namespaces": [
		{
			"domain": "Domain Name",
			"models": [],
			"enumerations": []
		}
	]
}
```

Application
```
{
	"namespaces": [
		{
			"domain": "Domain Name",
			"useCases": []
		}
	]
}
```

Infrastructure
```
{
	"namespaces": [
		{
			"domain": "Domain Name",
			"databaseSchema": "schema name",
			"models": []
		}
	]
}
```

Api
```
{
	"Routes": [
		{
			"namespace": "Namespace for the endpoint file",
			"Name": "Name of the endpoint file",
			"endpoints": []
		}
	]
}
```

## Recommendations

### Compiler Errors
Incomplete or inconsistent configuration in the json files can lead to build errors. 
Depending on the build error, three things can happen:
1. The error list displays the error and the generated file can be opened.
2. The error list displays the error, but the generated file cannot be opened.
3. The compiler crashes completely without displaying the exact reason in the error list.

Case 1)
This is the best-case scenario. As a rule, the errors are easy to identify and fix.

Case 2)
In this case, it is advisable to create a test project for the generator project (see example). This allows you to run each generator (project based) individually and view the generated files.

Case 3)
If this behavior occurs, the compiler has aborted the generation and was therefore unable to fill the error list. 
The solution is to set the build output from ***Minimal*** to ***Diagnostic*** and search the output for "Errors". It is recommended to start at the bottom and search backwards.
To make the output "shorter", the projects should be built individually until you find the project where the compiler aborts.

In Visual Studio, this setting can be found in the options.
* Tools -> Options -> Projects and Solutions -> Build and Run -> MSBuild project build output verbosity


### Update Generator NuGet Package
Due to Visual Studio, two different generator instances are maintained for compiling the project solution. One instance for the actual compilation (Build-Compiler) and a second instance for displaying the generated files in Visual Studio (Roslyn Language Service).
As a result, after updating the NuGet package, the build compiler may use a generator instance of the new version, but the Roslyn Language Service may still use the old version. 
Due to this behavior, you can activate in the json configuration files that the generator package version is inserted into the generated files. It is also possible that Visual Studio continues to use the old version for both compilers.
After updating the NuGet package, all Visual Studio instances should be closed and all processes with the name “VBCSCompiler” should be terminated. In most cases, this is sufficient. 
If the problem still occurs, you can repeat the process or restart the computer. Using Microsoft.CodeAnalysis.Generator in Visual Studio can sometimes be a little frustrating.

If none of this helps, you can delete the "Component Model Cache" folder. After that, Visual Studio will take a little longer to rebuild the cache when you start it for the first time.
1. Close all instances of Visual Studio.
2. Open Windows Explorer and navigate to: %localappdata%\Microsoft\VisualStudio\17.0_xxxx\ComponentModelCache (The 17.0_xxxx varies depending on the version/edition installed).
3. Delete the entire contents of this folder.