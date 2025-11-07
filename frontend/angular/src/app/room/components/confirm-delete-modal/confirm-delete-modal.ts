import { Component, input, output } from '@angular/core';

import { Button } from '../../../shared/components/button/button';
import { IconButton } from '../../../shared/components/icon-button/icon-button';
import { FocusTrap } from '../../../core/directives/focus-trap';
import { fadeIn } from '../../../utils/animations';
import { FADE_IN_ANIMATION_DURATION_MS } from '../../../app.constants';
import { AriaLabel, ButtonText, IconName } from '../../../app.enum';

@Component({
  selector: 'app-confirm-delete-modal',
  imports: [Button, IconButton, FocusTrap],
  templateUrl: './confirm-delete-modal.html',
  styleUrl: './confirm-delete-modal.scss',
  animations: [fadeIn(FADE_IN_ANIMATION_DURATION_MS)],
})
export class ConfirmDeleteModal {
  readonly participantName = input.required<string>();

  readonly confirmDelete = output<void>();
  readonly cancelDelete = output<void>();
  readonly closeModal = output<void>();

  public readonly closeIcon = IconName.Close;
  public readonly closeButtonAriaLabel = AriaLabel.Close;
  public readonly cancelButtonText = ButtonText.Cancel;
  public readonly deleteButtonText = ButtonText.Delete;

  public onConfirm(): void {
    this.confirmDelete.emit();
  }

  public onCancel(): void {
    this.cancelDelete.emit();
  }

  public onCloseModal(): void {
    this.closeModal.emit();
  }
}
