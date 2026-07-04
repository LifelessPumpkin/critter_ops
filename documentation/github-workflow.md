# GitHub Workflow

## Branch Strategy

CritterOps follows a simplified Git Flow workflow.

### Branches

#### main

The production branch.

main always represents the latest stable version of the application. Code should never be committed directly to this branch.

#### develop

The active development branch.

All feature branches are created from develop and merged back into develop through Pull Requests.

When a milestone or release is complete, develop is merged into main.

#### Feature Branches

Every GitHub Issue corresponds to exactly one feature branch.

Branch names begin with the issue number followed by a short description.

Examples:
```
1-scaffold-aspnet-core-skipper-api
2-scaffold-nextjs-gilligan-web
7-create-animal-entity
14-create-water-test-entity
```
Feature branches are always created from develop.

## Development Workflow

1. Create a GitHub Issue.
2. Create a branch from that issue.
3. Implement the work.
4. Open a Pull Request into develop.
5. Review and merge.
6. Repeat.

When a release is ready:

1. Merge develop into main.
2. Tag a release.
3. Deploy the application.

## Rules

* Never commit directly to main.
* Never commit directly to develop.
* One issue equals one branch.
* One branch should solve one problem.
* Pull Requests should remain focused and small whenever possible.