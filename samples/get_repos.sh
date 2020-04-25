#!/bin/bash

# bash and curl script to invoke the GitHub REST API to list my repos.
# Chris Joakim, Microsoft, 2020/04/25

curl -i https://api.github.com/users/cjoakim/repos > github_repos.json

