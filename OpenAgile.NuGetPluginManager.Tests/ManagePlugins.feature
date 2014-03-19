Feature: Manage plugins

Scenario 1: List available plugins
	Given a NuGet repository with plugins MyApp.Awesome.Plugin|1.0.0.0, MyApp.Awesome.Plugin|2.0.0.0
	And no plugins currently installed
	When requesting the list of available plugins
	Then I should see MyApp.Awesome.Plugin|1.0.0.0, MyApp.Awesome.Plugin|2.0.0.0