# sqllogger
A Rust plugin to log game events to MySQL

## MySQL Configuration

If you experience issues with large queries, you may need to increase the `max_allowed_packet` setting in your MySQL configuration. A recommended value is 134217728 bytes, which is 128 megabytes (MB).

```
max_allowed_packet = 134217728
```
