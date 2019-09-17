# Installation

> gauge install reportportal

Or manually download plugin from [releases](https://github.com/reportportal/agent-net-gauge/releases)

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
rp_launch_tags = tag1; tag2
```

Modify your `manifest.json` file and add `reportportal` plugin into plugins list.
