# Installation

## Offline installation
Download plugin from [releases](https://github.com/reportportal/agent-net-gauge/releases)

> gauge install reportportal --file <path_to_plugin_zip_file>

# Configuration
In you project add the following properties into `env/default/default.properties` file:
```yml
rp_uri = https://rp.epam.com/api/v1
rp_project = default_project
rp_uuid = 7853c7a9-7f27-43ea-835a-cab01355fd17
```

Modify your `manifest.json` file and add `reportportal` plugin into plugins list.
