Tracing is important in troubleshooting and debugging. The library emits traces at different levels (from low to high):
* Error 
* Warning 
* Information 
* Verbose 
* Frame

Except Frame level tracing, a higher level includes the previous level(s). By default Frame level tracing only outputs AMQP protocol headers and frames. It is the easiest way to read the AMQP traffic. However, it can be combined with other levels if you OR the Frame level with the other level(s) you need.

To enable tracing, set the trace level and a listener. For example, the following code writes AMQP frames to the system diagnostics trace in an .Net application.

```
Trace.TraceLevel = TraceLevel.Frame;
Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
```

By implementing the trace listener delegate, you can write the traces to your application's tracing module.

The library uses a conditional compilation symbol, TRACE, to enable/disable tracing calls. By default, this symbol is defined for both Debug and Release build. This means that the strings for the type/field names are included in the assembly. If you need to reduce the assembly size, you may remove the TRACE symbol and compile the project to get a smaller assembly (for NETMF devices).