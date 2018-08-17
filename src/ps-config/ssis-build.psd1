@{
   SolutionName="ssis-build"
   SqlVersion="130"
   SSASVersion="130"
   EnableNuGetPackageRestore=$true
   IgnorePackageVersioning=$False
   Nuget = @(
      @{
         Source = "http://nuget.laterooms.io/nuget"
         ApiKey = "creat10n"
      }
   );
}
