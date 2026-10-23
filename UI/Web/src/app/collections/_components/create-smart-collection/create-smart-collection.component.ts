import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {Router} from "@angular/router";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ToastrService} from '@openng/ngx-toastr';
import {finalize, tap} from "rxjs/operators";
import {form, FormField, required} from "@angular/forms/signals";
import {CollectionTagService} from "../../../_services/collection-tag.service";
import {FilterService} from "../../../_services/filter.service";
import {SmartFilter} from "../../../_models/metadata/v2/smart-filter";
import {FilterEntityType} from "../../../_models/metadata/v2/filter-entity-type";
import {LoadingComponent} from "../../../shared/loading/loading.component";
import {EmptyStateComponent} from "../../../shared/_components/empty-state/empty-state.component";

interface CreateSmartCollectionForm {
  title: string;
  smartFilterId: number | null;
}

@Component({
  selector: 'app-create-smart-collection',
  imports: [
    TranslocoDirective,
    FormField,
    LoadingComponent,
    EmptyStateComponent
  ],
  templateUrl: './create-smart-collection.component.html',
  styleUrl: './create-smart-collection.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateSmartCollectionComponent implements OnInit {
  private readonly modal = inject(NgbActiveModal);
  private readonly router = inject(Router);
  private readonly collectionService = inject(CollectionTagService);
  private readonly filterService = inject(FilterService);
  private readonly toastr = inject(ToastrService);

  isLoading = signal(true);
  isSaving = signal(false);
  seriesFilters = signal<SmartFilter[]>([]);

  formModel = signal<CreateSmartCollectionForm>({
    title: '',
    smartFilterId: null
  });
  formGroup = form(this.formModel, path => {
    required(path.title);
    required(path.smartFilterId);
  });

  ngOnInit() {
    this.filterService.getAllFilters().pipe(
      tap(filters => this.seriesFilters.set(filters.filter(f => f.entityType === FilterEntityType.Series))),
      finalize(() => this.isLoading.set(false))
    ).subscribe();
  }

  protected selectFilter(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.formModel.update(m => ({...m, smartFilterId: value ? parseInt(value, 10) : null}));
  }

  protected close() {
    this.modal.close();
  }

  protected submit() {
    if (!this.formGroup().valid()) return;

    const {title, smartFilterId} = this.formModel();
    if (smartFilterId === null) return;

    this.isSaving.set(true);
    this.collectionService.createSmartCollection(title, smartFilterId).pipe(
      finalize(() => this.isSaving.set(false))
    ).subscribe({
      next: collection => {
        this.toastr.success(translate('create-smart-collection-modal.created'));
        this.close();
        this.router.navigate(['/collections', collection.id]);
      },
      error: err => {
        this.toastr.error(err?.error?.message || translate('create-smart-collection-modal.generic-error'));
      }
    });
  }
}
