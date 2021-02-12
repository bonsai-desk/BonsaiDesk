$reactSpringPackageJSONs = Get-ChildItem -Path node_modules\\*@react-spring/\\*/package.json

foreach ($reactSpringPackageJSON in $reactSpringPackageJSONs) {
  (Get-Content $reactSpringPackageJSON) |
    Foreach-object { $_ -replace '"sideEffects": false', '"sideEffects": true' } |
    Set-Content $reactSpringPackageJSON
}