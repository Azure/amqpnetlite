## Contributing to the AMQP.Net Lite library
We encourage and welcome contributions from the development community. There are many ways to contribute.  
1. Open issues for any bugs or improvements you see in the library.  
2. Contributing bug fixes, examples and documentation.  
3. Contributing features. Features may be accepted but will need to be first be discussed.  

## Pre-Requisites for Contributing
1. Fork the repository into your GitHub account.
2. Clone your fork locally to your computer.
3. Configure a remote to "upstream" Azure/amqpnetlite repository so that you can keep your local repo up-to-date.
4. Get familiar with the code base and see what you can help.
5. Sign a Contribution License Agreement (one time activity) if you would like to open Pull Requests. This can be done after you open the first PR. Detailed instructions will be posted to help you sign the CLA.

## Basic Workflow for Contributions
1. All Pull Requests should be tracked by issues. For existing issue, please comment on the issue first to avoid duplication of effort. Open a new one if the issue does not exist.
2. Keep your local branch up-to-date.
4. Make your changes, test them, and commit them locally. If you created a new topic branch, you can commit as often as you need. However, the branch from which you open the PR should not contain many, small, and incomplete commits. Ideally any commit should build clean and pass the tests. If you do have such commits, consider squash merging them into master and open the PR there.
5. When you are happy with the changes, open a pull request.
6. Address code review comments, if any, and update your pull request.

## Coding Guidelines
1. Keep the code base consistent by following the existing conventions (e.g. naming, layout, etc.).
2. New source files should have the copyright header on the top. Please copy and paste the header from an existing source file. We do not accept code with a different copyright header.
3. New project output path should be set to "bin\$(Configuration)\$(MSBuildProjectName)\" and intermediate output path should be set to "obj\$(Configuration)\$(MSBuildProjectName)\" (relative to the solution directory).

Thank you for your contribution!