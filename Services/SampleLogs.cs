namespace LogInspector.Services;

/// <summary>Synthetic sample logs; contain no real data.</summary>
public static class SampleLogs
{
    public static string? Get(string? kind) => kind switch { "java" => Java, "dotnet" => DotNet, _ => null };

    private const string Java = """
2026-03-10 09:00:01.120  INFO 1 --- [main] com.example.demo.DemoApplication : Started DemoApplication in 4.2 seconds
2026-03-10 09:00:05.310  WARN 1 --- [http-nio-8080-exec-1] com.example.demo.OrderService : Retrying payment call (attempt 2)
2026-03-10 09:00:07.842 ERROR 1 --- [http-nio-8080-exec-2] com.example.demo.OrderRepository : Query failed
java.sql.SQLException: ORA-00942: table or view does not exist
	at oracle.jdbc.driver.T4CTTIoer.processError(T4CTTIoer.java:509)
	at com.example.demo.OrderRepository.findAll(OrderRepository.java:42)
	at com.example.demo.OrderService.list(OrderService.java:27)
2026-03-10 09:00:09.005 ERROR 1 --- [http-nio-8080-exec-3] com.example.demo.OrderController : Unhandled error
java.lang.NullPointerException: Cannot invoke "String.length()" because "name" is null
	at com.example.demo.OrderController.create(OrderController.java:58)
	at org.springframework.web.servlet.FrameworkServlet.service(FrameworkServlet.java:883)
Caused by: java.lang.IllegalStateException: no customer in session
	at com.example.demo.SessionHelper.require(SessionHelper.java:19)
	... 2 more
2026-03-10 09:01:11.400 ERROR 1 --- [http-nio-8080-exec-4] com.example.demo.OrderController : Unhandled error
java.lang.NullPointerException: Cannot invoke "String.length()" because "name" is null
	at com.example.demo.OrderController.create(OrderController.java:58)
2026-03-10 09:02:00.000 DEBUG 1 --- [scheduling-1] com.example.demo.Cleaner : Removed 0 expired sessions
""";

    private const string DotNet = """
2026-03-10 10:00:00 INFO Now listening on: http://localhost:5000
2026-03-10 10:00:03 WARN Cache miss ratio high (0.62)
2026-03-10 10:00:05 ERROR Request failed
System.NullReferenceException: Object reference not set to an instance of an object.
   at Demo.Services.CartService.Total(Cart cart) in C:\src\Demo\CartService.cs:line 31
   at Demo.Controllers.CartController.Get(Int32 id) in C:\src\Demo\CartController.cs:line 18
2026-03-10 10:00:08 ERROR Database update failed
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
 ---> Oracle.ManagedDataAccess.Client.OracleException: ORA-00001: unique constraint violated
   at Oracle.ManagedDataAccess.Client.OracleCommand.ExecuteNonQuery()
   --- End of inner exception stack trace ---
   at Microsoft.EntityFrameworkCore.DbContext.SaveChanges()
2026-03-10 10:00:20 ERROR Request failed
System.NullReferenceException: Object reference not set to an instance of an object.
   at Demo.Services.CartService.Total(Cart cart) in C:\src\Demo\CartService.cs:line 31
2026-03-10 10:00:30 DEBUG Heartbeat ok
""";
}
