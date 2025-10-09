# StripperSharp

A C# port of [Striper:Source](https://github.com/alliedmodders/stripper-source) & [StripperCS2](https://github.com/Source2ZE/StripperCS2) powered by [ModSharp](https://github.com/Kxnrl/modsharp-public).

## Document

- [Wiki](https://github.com/fyscs/cs2/blob/master/.fys/Stripper.md)  
- [StripperCS2 Doc](https://github.com/Source2ZE/StripperCS2/blob/master/README.md)

## Note

- Please use valid ``RFC 7159``/``ECMA-262`` JSON format, duplicate keys will not work.  
- For user friendly and preventing misoperation, ``replace`` is disabled by default, cvar ``ms_stripper_replace_enabled``.  
- Support both ``remove`` and ``filter``.
- Remove TargetType and make it default to ``EntityNameOrClassName``.
- ``global.jsonc`` for every entity lump, ``global_default.jsonc`` only for default_ents lump.  
- Unlike StriperCS2, single object style is not support currently, PR is welcome.

## ConVars

- ``ms_stripper_replace_enabled``: Enabled ``replace`` block in ``modify`` section, default: ``false``.  
- ``ms_stripper_verbose_enabled``: Enabled verbose logging, default: ``false``.  
