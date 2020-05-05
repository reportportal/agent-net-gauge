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

rp_enabled = true/false
```

Modify your `manifest.json` file and add `reportportal` plugin into plugins list if it is not there yet.

# Upgrading after Preview version

If you have already installed previe version and want to install stable version, firstly you need uninstall plugin and install it again.

> gauge uninstall reportportal

# Troubleshooting

Set `ReportPortal_TraceLevel` property in `env/default/default.properties` file to `Verbose` value. Execute tests and find logs in default `logs` directory.