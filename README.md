# Installation

> gauge install reportportal

Or manually download plugin from [releases](https://github.com/reportportal/agent-net-gauge/releases) tab.

And then:
> gauge install reportportal --file <path_to_plugin_zip_file>

# Configuration

In you project add the following properties into `env/default/default.properties` file:
```yml
rp_uri = https://rp.epam.com/api/v1
rp_project = default_project
rp_uuid = 7853c7a9-7f27-43ea-835a-cab01355fd17

#optional
rp_launch_name = My Super Project
rp_launch_description = This is description
rp_launch_attributes = tag1; tag2; platform:x64
rp_launch_debugmode = true/false

rp_enabled = true/false
```

Discover [more](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md) about configuration.

Modify your `manifest.json` file and add `reportportal` plugin into plugins list if it is not there yet.

# Upgrading after Preview version

If you have already installed preview version and want to install stable version, firstly you need uninstall plugin and install it again.

> gauge uninstall reportportal

# Troubleshooting

Set `ReportPortal_TraceLevel` property in `env/default/default.properties` file to `Verbose` value. Execute tests and find logs in default `logs` directory.

# License
ReportPortal is licensed under [Apache 2.0](https://github.com/reportportal/agent-net-nunit/blob/master/LICENSE)

We use Google Analytics for sending anonymous usage information as library's name/version and the agent's name/version when starting launch. This information might help us to improve integration with ReportPortal. Used by the ReportPortal team only and not for sharing with 3rd parties. You are able to [turn off](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md#analytics) it if needed.