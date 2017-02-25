#### 2.1.1 (Released 2017/02/25)
* Fixed an issue with Powershell passing empty strings instead of nulls in parameters
* Replaced parameter type for EraseSensitiveInfo from bool to SwitchParameter in Publish-SsisDeploymentPackage powershell CmdLet

#### 2.1.0 (Released 2017/02/22)
* Added Powershell functionality

#### 2.0.0 (Released 2017/02/15)
* Completely removed Microsoft DTS dependencies from the library. The tool can work standalone with no need to install any dependencies
* Added SsisDeploy application which is capable of deploying any ispac deployment packages
* Added unit tests with 100% code coverage
* Removed SensitiveParameter Build Argument because it was causing runtime issues

#### 1.3.0 (Released 2017/01/11)
* Fixed an issue with Project connections decryption. 

#### 1.2.0 (Released 2017/01/10)
* Fixed an issue with EncryptAllByPassword not finding encrypted node
* Added FAKE build
* Added RELEASE_NOTES
* Changed nuget package folder from app to tools