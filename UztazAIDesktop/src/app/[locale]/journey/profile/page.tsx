import { IntakeWizardForm } from "@/components/journey/intake-wizard-form";

// §12 Addendum 2: the Placement & Eligibility Intake Wizard replaces the old flat Profile
// survey as the product's true first-run entry point. Kept at the same "/journey/profile" route
// (rather than renaming it) so the journey stepper, command palette and post-auth redirects
// don't need a structural route change — only what renders here changed.
export default function ProfilePage() {
  return <IntakeWizardForm />;
}
