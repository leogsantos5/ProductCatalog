Feature: Concurrent editing
  Back-office users edit products. Someone who saves using the version they retrieved must not
  silently overwrite a change that was made in the meantime.

  Background:
    Given a product "Lens" priced 89.90
    And "Lens" has been retrieved

  Scenario: Saving with an up-to-date version succeeds
    When the price of "Lens" is changed to 99.90 using the retrieved version
    Then the request succeeds
    And "Lens" is priced 99.90
    And a new version of "Lens" is returned

  Scenario: Saving over someone else's change is rejected
    Given another user has changed the price of "Lens" to 79.90
    When the price of "Lens" is changed to 99.90 using the retrieved version
    Then the request is rejected with status 412 and error code "VERSION_MISMATCH"
    And "Lens" is priced 79.90

  Scenario: A sale also makes the retrieved version out of date
    Given 1 unit of "Lens" has been sold
    When the price of "Lens" is changed to 99.90 using the retrieved version
    Then the request is rejected with status 412 and error code "VERSION_MISMATCH"

  Scenario: Two users saving at the same moment
    When two users change the price of "Lens" at the same time using the retrieved version
    Then 1 request succeeds and 1 is rejected with error code "VERSION_MISMATCH"

  Scenario: Saving without a version keeps the last change
    Given another user has changed the price of "Lens" to 79.90
    When the price of "Lens" is changed to 99.90 without a version
    Then the request succeeds
    And "Lens" is priced 99.90

  Scenario: Deleting a product that changed since it was retrieved is rejected
    Given another user has changed the price of "Lens" to 79.90
    When "Lens" is deleted using the retrieved version
    Then the request is rejected with status 412 and error code "VERSION_MISMATCH"
    And "Lens" still exists

  Scenario: Deleting with an up-to-date version succeeds
    When "Lens" is deleted using the retrieved version
    Then the request succeeds
    And "Lens" no longer exists
