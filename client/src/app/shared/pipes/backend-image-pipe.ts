import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

@Pipe({
  name: 'backendImage',
  standalone: true
})
export class BackendImagePipe implements PipeTransform {

  transform(value: string | undefined): string {
    if (!value) return '';
    if (value.startsWith('http')) return value;
    
    // In production, backend images (which may be stored as relative paths in the DB) 
    // are served from Azure Blob Storage.
    if (environment.production) {
      const filename = value.split('/').pop();
      return `https://fashionassetsoe.blob.core.windows.net/images/${filename}`;
    }

    const baseUrl = environment.baseUrl.replace('api/', '');
    return baseUrl + value.replace(/^\//, '');
  }

}
