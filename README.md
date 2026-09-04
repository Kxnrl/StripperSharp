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
- If any configuration file fails to parse, the **whole** configuration is discarded and every error is logged.
  Two files resolving to the same ``world::lump`` key (e.g. ``maps/X/foo.jsonc`` and ``maps/X/X/foo.jsonc``)
  count as such an error.
- In ``match``, every entry of ``connections`` is an independent requirement: the entity must own **at least
  one** connection matching it. Listing two entries means "has a connection matching A **and** a connection
  matching B", not "one connection matching both".
- In ``delete``, every entry of ``connections`` is applied on its own and removes **every** connection it
  matches.
- Wildcard (trailing ``*``) matching is case-insensitive. It is only honoured for ``targetname``/``classname``
  in ``match``, for ``output``/``param`` in a connection entry, and for any key in ``delete``.
- ``replace`` only handles key/value pairs. A ``connections`` (or ``io``) key inside ``replace`` is a
  configuration error; express IO rewrites with ``delete`` + ``insert`` instead.
- ``replace`` only rewrites keys the entity **already has**. A key that is not present is skipped with a warning
  and is never created; use ``insert`` for that (``insert`` is add-or-set).
- An ``add``/``insert`` connection missing ``output``, ``input`` or ``target`` is skipped with a warning when the
  map loads; an entry with all three missing is rejected when the configuration is read.

## ConVars

- ``ms_stripper_replace_enabled``: Enabled ``replace`` block in ``modify`` section, default: ``false``.  
- ``ms_stripper_verbose_enabled``: Enabled verbose logging, default: ``false``.  

## Installation

- Download file from latest [Release](https://github.com/Kxnrl/StripperSharp/releases/latest)
- Extract files to `game/sharp` and merge into `gamedata` and `module` folder.
