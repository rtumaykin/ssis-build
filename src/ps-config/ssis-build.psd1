@{
   SolutionName="ssis-build"
   SSISDatabaseName="SSISDB"
   SqlVersion="130"
   EnableNuGetPackageRestore=$true
   Nuget = @(
      @{
         Source = "http://nuget.laterooms.io/nuget"
         ApiKey = "creat10n"
      }
   );
   Dev = @(
      @{
         IncludeCompositeObjects=$false
         Server="."
         ConnectionString="Server=.;Database=laterooms;connection timeout=6000;Integrated Security=SSPI;"
         Testing = @(
            @{
                UseEnvironmental = $false
                RunTimeOut = 0
                TestTimeOut = 0
            }
         )
       }
   );
   CI = @(
      @{
         IncludeCompositeObjects=$false
         Server="dev-sql-01"
         ConnectionString="Server=dev-sql-01;Database=laterooms;connection timeout=6000;Integrated Security=SSPI;"
         Testing = @(
            @{
                UseEnvironmental = $false
                RunTimeOut = 0
                TestTimeOut = 0
            }
         )
       }
   ); 
   QA = @(
      @{
         IncludeCompositeObjects=$false
         Server="DBTEST01"
         ConnectionString="Server=DBTEST01;Database=Laterooms;connection timeout=6000;Integrated Security=SSPI;"
         Testing = @(
            @{
                UseEnvironmental = $false
                RunTimeOut = 0
                TestTimeOut = 0
            }
         )
       }
   );
}