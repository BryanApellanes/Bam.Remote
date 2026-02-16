# bam.remote

SSH-based remote host management library for file deployment, command execution, and Linux user/group administration.

## Overview

bam.remote provides a comprehensive toolkit for managing remote Linux hosts over SSH. It is built on the SSH.NET library and organized into two major subsystems: **Deployment** for copying files and executing commands on remote hosts, and **Etc** for managing Linux `/etc/passwd`, `/etc/shadow`, and `/etc/group` files programmatically.

The Deployment subsystem centers on `RemoteSshHost`, which wraps SSH and SFTP clients to provide file upload (with parallel transfer, retry logic, and remote directory creation), command execution, and user management. `DeploymentManager` orchestrates higher-level workflows: copying individual files or entire directories to a remote host, and performing deployments that combine file transfers with remote command execution via `nohup`. `ManagedFileSet` and `ManagedFile` model the mapping between local files/directories and their remote counterparts, supporting re-rooting of file paths for deployment to different remote directory structures.

The Etc subsystem provides a full object model for Linux credential files. `PasswdFile`, `ShadowFile`, and `GroupFile` can parse, manipulate, and serialize their respective `/etc/` files. `EtcCredentialManager` ties them together, providing operations to add users (with automatic UID assignment), set passwords (using SHA-512 hashing via CryptSharp), manage groups, and save the modified files back. `ShadowPassword` handles the full shadow password format including algorithm identifiers, salt generation, and hash computation.

## Key Classes

### Deployment

| Class | Description |
|---|---|
| `DeploymentManager` | High-level orchestrator for remote deployments. Provides `CopyFile`, `CopyDirectory`, `Deploy` (copy + remote command execution), and `EnsureDirectory` operations. |
| `RemoteSshHost` | Core SSH/SFTP host wrapper. Provides `Copy` (parallel file upload with retry), `Execute` (run SSH commands), `ListUsers`, `AddUser`, `DeleteUser`, `EnsureDirectory`, and `RemoteFileExists`. Fires events for copy lifecycle. |
| `RemoteSshHostInfo` | Data class holding SSH connection details: hostname, port, username. Default username is `"bambot"`, default port is `22`. |
| `DeploymentSet` | Groups multiple `ManagedFileSet` instances with a `RemoteCommand` to execute after copying. |
| `ManagedFileSet` | Represents a collection of local files and directories to be copied to a remote host. Supports re-rooting paths to a remote directory. Factory methods: `FromFile`, `FromDirectory`, `FromDescriptor`. |
| `ManagedFile` | Maps a single local file to its remote file path. Supports fluent `From`/`To`/`CopyTo` API and path re-rooting via `GetReRootedFile`. |
| `ManagedFileSetDescriptor` | Serializable data descriptor for `ManagedFileSet` (comma/semicolon-delimited directory and file lists). |
| `RemoteSshHostCopyArgs` | Event args for copy operations, carrying the current `ManagedFile`, `ManagedFileSet`, exception info, and retry status. |
| `DeterministicSshHostPasswordGenerator` | Generates deterministic passwords from host identity + timestamp using HMAC hashing. Core `Generate` methods are commented out pending `ManagedPassword` implementation. |
| `SshHostIdentifier` | Identifies a host by hostname and MAC address. |
| `SshHostCredentials` | Data class for SSH credentials. |

### Etc (Linux credential file management)

| Class | Description |
|---|---|
| `EtcCredentialManager` | Manages `/etc/passwd`, `/etc/shadow`, and `/etc/group` as a unit. Provides `AddUser`, `SetPassword`, `AddGroup`, `AddGroupMembers`, `Save`, and user/group existence checks. |
| `PasswdFile` | Parses and serializes `/etc/passwd`. Supports `AddUser` with automatic UID assignment and lookup by username or UID. |
| `ShadowFile` | Parses and serializes `/etc/shadow`. Supports `AddEntry`, `SetPassword`, and lookup by username. |
| `GroupFile` | Parses and serializes `/etc/group`. Supports `AddGroup`, `AddGroupMember`, and group existence checks. |
| `PasswdEntry` | Represents a single line in `/etc/passwd`: username, UID, GID, home directory, shell. |
| `ShadowEntry` | Represents a single line in `/etc/shadow`: username, password hash, last changed, min/max days, warn, inactive, expire. |
| `GroupEntry` | Represents a single line in `/etc/group`: group name, GID, member list. |
| `ShadowPassword` | Full shadow password model with algorithm, salt, and hash. Supports SHA-512 (default), SHA-256, Blowfish, and MD5. Salt generation uses CryptSharp. |
| `CrypterPasswordSetter` | Default `IShadowPasswordSetter` using CryptSharp's `Crypter.Sha512`. |
| `OpenSslShadowPasswordSetter` | Alternative `IShadowPasswordSetter` that shells out to `openssl passwd` for hash computation. |
| `EtcUser` | Return type for user operations, carrying username, password, and credential manager reference. |
| `EtcGroup` | Return type for group operations, carrying group name, GID, and credential manager reference. |
| `EpochDay` | Represents days since Unix epoch (1970-01-01), used for shadow file date fields. |

## Dependencies

**Project References:**
- `bam.base` -- core primitives (logging, extensions, `Args` validation, `Instant`, `HashAlgorithms`)
- `bam.data.repositories` -- data repository infrastructure
- `bam.data` -- data layer

**NuGet Packages:**
- `SSH.NET` 2025.1.0 -- SSH and SFTP client library
- `CryptSharp.NET` 7.0.1 -- password hashing (SHA-512, Blowfish, etc.)

**Target Framework:** net10.0
**NuGet Package:** Published as `bam.remote` (v2.0.0, author: Bryan Apellanes, company: Three Headz)

## Usage Examples

### Copy a directory to a remote host

```csharp
using Bam.Remote.Deployment;

var hostInfo = new RemoteSshHostInfo("192.168.1.100")
{
    UserName = "deploy",
    Port = 22
};

var manager = new DeploymentManager(hostInfo);
manager.CopyDirectory("/local/build/output", "/opt/myapp");
```

### Deploy files and run a command

```csharp
using Bam.Remote.Deployment;

var host = new RemoteSshHost
{
    HostName = "192.168.1.100",
    Port = 22,
    LoginUserName = "deploy",
    LoginPassword = "secret"
};

var deploymentSet = new DeploymentSet { RemoteCommand = "/opt/myapp/start.sh" }
    .AddFileSet(ManagedFileSet.FromDirectory("/local/build/output", "/opt/myapp"));

var manager = new DeploymentManager(new RemoteSshHostInfo("192.168.1.100"));
manager.Deploy(host, deploymentSet);
```

### Execute a remote SSH command

```csharp
using Bam.Remote.Deployment;

string result = RemoteSshHost.Execute("192.168.1.100", 22, "admin", "password", "uptime");
Console.WriteLine(result);
```

### Manage Linux users via /etc files

```csharp
using Bam.Remote.Etc;

// Load credential files from a local copy of /etc
var manager = new EtcCredentialManager("/path/to/etc/");

// Add a user with password
EtcUser user = manager.AddUser("newuser", "securepassword");

// Add a group and assign the user
EtcGroup group = manager.AddGroup("developers", "newuser");

// Save changes back
manager.Save("/path/to/etc/");
```

### Parse and modify shadow passwords

```csharp
using Bam.Remote.Etc;

ShadowFile shadow = ShadowFile.Load("/etc/shadow");
ShadowEntry entry = shadow.SetPassword("existinguser", "newpassword");
shadow.Save("/etc/shadow");
```

## Known Gaps / Not Yet Implemented

- **`ManagedPassword` implementation is incomplete.** Multiple methods throw `NotImplementedException("ManagedPassword implementation is incomplete")`:
  - `EtcCredentialManager.AddUser` -- creates passwd, group, and shadow entries but cannot return `EtcUser` because `ManagedPassword` is not available.
  - `EtcCredentialManager.SetPassword` -- same issue.
  - `RemoteSshHost.FromHostInfo` -- cannot convert `RemoteSshHostInfo` to `RemoteSshHost` because `ManagedPassword.Show()` is unavailable.
  - `DeterministicSshHostPasswordGenerator.Generate` methods are commented out entirely.
- **`RemoteSshHost.ListGroups`** -- Throws `NotImplementedException`.
- **`RemoteSshHost.DeleteGroup`** -- Throws `NotImplementedException`.
- **`RemoteSshHost.Upload`** -- Throws `NotImplementedException`. Single-file upload (outside of `ManagedFileSet` copy) is not implemented.
- **`RemoteSshHost.Download`** -- Throws `NotImplementedException`. File download from remote hosts is not supported.
- **`RemoteSshHost.AddUser`** (the SSH-based version) -- Throws `NotImplementedException("This method is not properly implemented")`. Has a TODO comment: "fix this implementation to modify files directly or set sudo to not require password on the host or both."
- **`DeploymentSet.AddFileSet`** -- Contains a bug: creates a new `HashSet` from `FileSets` but does not add the `managedFileSet` parameter to it before converting back to array. The added file set is silently dropped.
- **`RemoteSshHost.RemoteFileExists` (3-parameter overload without `out`)** -- Contains an infinite recursion bug: calls itself instead of the 4-parameter overload with the `out` parameter.
