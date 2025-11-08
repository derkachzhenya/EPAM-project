terraform {
  backend "s3" {
    bucket       = "epam-project-tfstate-61812246"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
