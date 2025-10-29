$profiles = @("idna-amd-win-relay", "idna-intel-win-relay", "idna-amd-lin-relay", "idna-intel-lin-relay")
$sslProtocols = @("tls12", "tls13")

foreach ($profile in $profiles) {
    foreach ($sslProtocol in $sslProtocols) {
        $csvFile = "${profile}-${sslProtocol}.csv"
        
        if (Test-Path $csvFile) {
            Write-Host "Skipping: $csvFile already exists"
            continue
        }
        
        $cmd = @(
            "crank",
            "--config https://github.com/aspnet/Benchmarks/blob/main/scenarios/tls.benchmarks.yml?raw=true",
            "--profile $profile",
            "--scenario tls-handshakes-httpsys",
            "--relay",
            "--config https://github.com/aspnet/Benchmarks/blob/main/build/azure.profile.yml?raw=true",
            "--application.framework net10.0",
            "--application.aspNetCoreVersion 10.0.0-rtm.25512.102",
            "--application.runtimeVersion 10.0.0-rtm.25512.102",
            "--application.sdkVersion 10.0.100-rtm.25512.102",
            "--application.noclean true",
            "--application.options.reuseBuild true",
            "--load.variables.sslProtocol $sslProtocol",
            "--csv $csvFile"
        ) -join " "
        Write-Host "Running: $cmd"
        Invoke-Expression $cmd
    }
}